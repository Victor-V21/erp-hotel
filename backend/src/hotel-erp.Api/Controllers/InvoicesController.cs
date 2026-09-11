using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using AutoMapper;
using hotel_erp.Api.Dtos.Common;
using hotel_erp.Api.Services.Interfaces;
using hotel_erp.Api.Services;
using hotel_erp.Api.Database.Entities;
using hotel_erp.Api.Database;
using hotel_erp.Api.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace hotel_erp.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class InvoicesController : ControllerBase
    {
        private readonly IInvoiceRepository _repo;
        private readonly ICAIRepository _caiRepo;
        private readonly ICustomerRepository _customerRepo;
        private readonly IAccountingService _accountingService;
        private readonly IFiscalAuthorizationService _fiscalAuthorizationService;
        private readonly FiscalProfileService _fiscalProfileService;
        private readonly IdempotencyService _idempotencyService;
        private readonly ApplicationDbContext _context;
        private readonly AuditService _auditService;
        private readonly IMapper _mapper;

        public InvoicesController(
            IInvoiceRepository repo,
            ICAIRepository caiRepo,
            ICustomerRepository customerRepo,
            IAccountingService accountingService,
            IFiscalAuthorizationService fiscalAuthorizationService,
            FiscalProfileService fiscalProfileService,
            IdempotencyService idempotencyService,
            ApplicationDbContext context,
            AuditService auditService,
            IMapper mapper)
        {
            _repo = repo;
            _caiRepo = caiRepo;
            _customerRepo = customerRepo;
            _accountingService = accountingService;
            _fiscalAuthorizationService = fiscalAuthorizationService;
            _fiscalProfileService = fiscalProfileService;
            _idempotencyService = idempotencyService;
            _context = context;
            _auditService = auditService;
            _mapper = mapper;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<InvoiceDto>>> GetAll()
            => Ok(_mapper.Map<IEnumerable<InvoiceDto>>(await _repo.GetAllAsync()));

        [HttpGet("{id}")]
        public async Task<ActionResult<InvoiceDto>> GetById(Guid id)
        {
            var invoice = await _repo.GetByIdAsync(id);
            if (invoice == null) return NotFound();
            return Ok(_mapper.Map<InvoiceDto>(invoice));
        }

        [HttpGet("search")]
        public async Task<ActionResult<IEnumerable<InvoiceDto>>> Search([FromQuery] string? dni, [FromQuery] DateTime? start, [FromQuery] DateTime? end, [FromQuery] Guid? caiId, [FromQuery] Guid? documentAuthorizationId, [FromQuery] Guid? originalInvoiceId)
        {
            if (originalInvoiceId.HasValue)
                return Ok(_mapper.Map<IEnumerable<InvoiceDto>>(await _repo.GetByOriginalInvoiceAsync(originalInvoiceId.Value)));
            if (caiId.HasValue || documentAuthorizationId.HasValue)
                return Ok(_mapper.Map<IEnumerable<InvoiceDto>>(await _repo.GetByAuthorizationAsync(caiId, documentAuthorizationId)));
            if (!string.IsNullOrEmpty(dni))
                return Ok(_mapper.Map<IEnumerable<InvoiceDto>>(await _repo.GetByGuestDocumentAsync(dni)));
            if (start.HasValue && end.HasValue)
                return Ok(_mapper.Map<IEnumerable<InvoiceDto>>(await _repo.GetByDateRangeAsync(start.Value, end.Value)));
            return Ok(_mapper.Map<IEnumerable<InvoiceDto>>(await _repo.GetAllAsync()));
        }

        [HttpGet("date-range")]
        public async Task<ActionResult<IEnumerable<InvoiceDto>>> GetByDateRange([FromQuery] DateTime start, [FromQuery] DateTime end)
            => Ok(_mapper.Map<IEnumerable<InvoiceDto>>(await _repo.GetByDateRangeAsync(start, end)));

        [HttpPut("{id}")]
        [Authorize(Policy = PermissionNames.CreateInvoices)]
        public async Task<ActionResult<InvoiceDto>> Update(Guid id, [FromBody] CreateInvoiceRequest request)
        {
            var invoice = await _repo.GetByIdAsync(id);
            if (invoice == null) return NotFound();
            if (invoice.DocumentType is InvoiceDocumentType.Factura or InvoiceDocumentType.NotaCredito or InvoiceDocumentType.NotaDebito)
                return BadRequest("Un documento fiscal emitido no puede modificarse; use el documento de ajuste correspondiente");
            if (invoice.Status == InvoiceStatus.Anulada)
                return BadRequest("No se puede modificar una factura anulada");

            await using var transaction = await _context.Database.BeginTransactionAsync();

            decimal subtotal = 0, isvAmount = 0, touristTax = 0, discounts = 0;

            var items = request.Items.Select(item =>
            {
                var lineTotal = item.Quantity * item.UnitPrice;
                var discount = lineTotal * (item.DiscountPercentage / 100m);
                var lineAfterDiscount = lineTotal - discount;
                subtotal += lineAfterDiscount;
                if (!item.IsExempt) isvAmount += lineAfterDiscount * item.ISVRate;
                if (item.IsTouristTaxable) touristTax += lineAfterDiscount * 0.04m;
                discounts += discount;

                return new InvoiceItem
                {
                    InvoiceId = id,
                    Description = item.Description,
                    Quantity = item.Quantity,
                    UnitPrice = item.UnitPrice,
                    LineTotal = lineTotal,
                    IsExempt = item.IsExempt,
                    ISVRate = item.ISVRate,
                    IsTouristTaxable = item.IsTouristTaxable,
                    DiscountPercentage = item.DiscountPercentage
                };
            }).ToList();

            invoice.CustomerName = request.CustomerName;
            invoice.RTNCliente = request.RTNCliente;
            invoice.CustomerAddress = request.CustomerAddress;
            invoice.SubTotal = Math.Round(subtotal, 2);
            invoice.ISVAmount = Math.Round(isvAmount, 2);
            invoice.TouristTaxAmount = Math.Round(touristTax, 2);
            invoice.DiscountsAmount = Math.Round(discounts, 2);
            invoice.TotalAmount = Math.Round(subtotal + isvAmount + touristTax, 2);

            // Remove old items and add new ones
            await _repo.DeleteInvoiceItemsAsync(id);
            foreach (var item in items)
                invoice.InvoiceItems.Add(item);

            await _repo.UpdateAsync(invoice);
            var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            await _auditService.LogAsync(userId, "UpdateInvoice", nameof(Invoice), invoice.Id, new { invoice.CorrelativeNumber, invoice.SubTotal, invoice.TotalAmount }, invoice.CorrelativeNumber);
            await transaction.CommitAsync();
            return Ok(_mapper.Map<InvoiceDto>(invoice));
        }

        [HttpPost]
        [Authorize(Policy = PermissionNames.CreateInvoices)]
        public async Task<ActionResult<InvoiceDto>> Create(
            [FromBody] CreateInvoiceRequest request,
            [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey)
        {
            if (!IdempotencyService.TryNormalizeKey(idempotencyKey, out var normalizedKey))
                return BadRequest(new ProblemDetails
                {
                    Type = "https://httpstatuses.com/400",
                    Title = "Clave de idempotencia inválida",
                    Status = StatusCodes.Status400BadRequest,
                    Detail = "La cabecera Idempotency-Key es obligatoria y debe contener un UUID válido."
                });

            var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var requestHash = IdempotencyService.ComputeRequestHash(request);
            await using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                var priorAttempt = await _idempotencyService.LockAndFindAsync(
                    userId,
                    "invoice:create",
                    normalizedKey,
                    requestHash,
                    HttpContext.RequestAborted);

                if (priorAttempt is not null)
                {
                    var existingInvoice = await _repo.GetByIdAsync(priorAttempt.ResourceId);
                    if (existingInvoice is null)
                        return Conflict(new ProblemDetails
                        {
                            Type = "https://httpstatuses.com/409",
                            Title = "Resultado idempotente inconsistente",
                            Status = StatusCodes.Status409Conflict,
                            Detail = "La operación ya fue registrada, pero su factura no está disponible."
                        });

                    await transaction.CommitAsync();
                    Response.Headers["Idempotency-Key"] = normalizedKey;
                    return CreatedAtAction(nameof(GetById), new { id = existingInvoice.Id }, _mapper.Map<InvoiceDto>(existingInvoice));
                }
            }
            catch (IdempotencyConflictException ex)
            {
                return Conflict(new ProblemDetails
                {
                    Type = "https://httpstatuses.com/409",
                    Title = "Conflicto de idempotencia",
                    Status = StatusCodes.Status409Conflict,
                    Detail = ex.Message
                });
            }

            var profileBlockReason = await _fiscalProfileService.GetOperationBlockReasonAsync();
            if (profileBlockReason is not null)
                return FiscalProfileConflict(profileBlockReason);

            if (!string.Equals(request.DocumentType, InvoiceDocumentType.Factura.ToString(), StringComparison.Ordinal))
                return BadRequest("Este endpoint solo emite facturas; use el flujo específico para notas fiscales");

            Folio? settlementFolio = null;
            Discount? appliedDiscount = null;
            var settlementItems = new List<FolioItem>();
            if (request.FolioId.HasValue)
            {
                var folioLock = $"folio-settlement:{request.FolioId.Value:N}";
                await _context.Database.ExecuteSqlInterpolatedAsync(
                    $"SELECT pg_advisory_xact_lock(hashtext({folioLock}));",
                    HttpContext.RequestAborted);

                settlementFolio = await _context.Folios
                    .Include(folio => folio.FolioItems)
                    .Include(folio => folio.Reservation)
                    .Include(folio => folio.Room)
                    .SingleOrDefaultAsync(folio => folio.Id == request.FolioId.Value);
                if (settlementFolio is null)
                    return NotFound("Folio no encontrado");
                if (settlementFolio.Status != FolioStatus.Abierto
                    || settlementFolio.Reservation.Status != ReservationStatus.CheckIn)
                    return Conflict("El folio no se encuentra abierto para liquidación");
                if (!request.GuestId.HasValue || request.GuestId.Value != settlementFolio.GuestId)
                    return BadRequest("El huésped de la factura no coincide con el folio");
                if (await _context.Invoices.AnyAsync(invoice => invoice.FolioId == settlementFolio.Id))
                    return Conflict("El folio ya tiene una factura de liquidación");

                settlementItems = settlementFolio.FolioItems
                    .OrderBy(item => item.CreatedAt)
                    .ThenBy(item => item.Id)
                    .ToList();
                if (settlementItems.Count == 0)
                    return BadRequest("El folio no contiene cargos para facturar");

                if (request.DiscountId.HasValue)
                {
                    appliedDiscount = await _context.Discounts
                        .SingleOrDefaultAsync(discount => discount.Id == request.DiscountId.Value);
                    if (appliedDiscount is null || !appliedDiscount.IsActive)
                        return BadRequest("El descuento seleccionado no existe o no está activo");
                    if (appliedDiscount.DiscountType != DiscountType.Porcentaje)
                        return BadRequest("La liquidación del folio solo admite descuentos porcentuales");
                    if (appliedDiscount.Value is < 0m or > 100m)
                        return BadRequest("El porcentaje del descuento configurado no es válido");
                }
            }
            else if (request.DiscountId.HasValue
                || request.CashRegisterId.HasValue
                || request.CashReceived.HasValue
                || !string.IsNullOrWhiteSpace(request.PaymentReference))
            {
                return BadRequest("Caja y descuento de liquidación solo pueden indicarse al facturar un folio");
            }

            var cai = await _caiRepo.GetByIdAsync(request.CAIId);
            if (cai == null) return BadRequest("CAI no encontrado");
            if (cai.Status != CAIStatus.Activo) return BadRequest("El CAI no está activo");
            if (cai.DueDate < HondurasTime.Today) return BadRequest("El CAI está vencido");

            const InvoiceDocumentType documentType = InvoiceDocumentType.Factura;
            FiscalCorrelativeResult? authorizationResult = null;
            var correlative = string.Empty;
            try
            {
                if (request.DocumentAuthorizationId.HasValue)
                {
                    authorizationResult = await _fiscalAuthorizationService.GetNextCorrelativeAsync(documentType, request.DocumentAuthorizationId);
                    correlative = authorizationResult.CorrelativeNumber;
                }
                else
                {
                    correlative = await _repo.GetNextCorrelativeAsync(request.CAIId);
                }
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }

            var taxpayerType = Enum.TryParse<TaxpayerType>(request.TaxpayerType, out var parsedType)
                ? parsedType
                : TaxpayerType.ConsumidorFinal;
            var isIsvExempt = taxpayerType == TaxpayerType.Exonerado && request.IsIsvExempt;
            var isTouristTaxExempt = taxpayerType == TaxpayerType.Exonerado && request.IsTouristTaxExempt;
            if (taxpayerType == TaxpayerType.Exonerado
                && (string.IsNullOrWhiteSpace(request.ExonerationOrderNumber) || string.IsNullOrWhiteSpace(request.SefinExonerationCertificateNumber)))
                return BadRequest("Cliente exonerado requiere O.C. Exenta y Constancia SEFIN");

            InvoiceCalculation calculation;
            try
            {
                var calculationLines = settlementFolio is null
                    ? request.Items.Select(ToCalculationLine)
                    : settlementItems.Select(item => new InvoiceLineInput(
                        item.Description,
                        item.Quantity,
                        item.UnitPrice,
                        item.IsExempt,
                        item.ISVRate,
                        item.IsTouristTaxable,
                        CombineDiscountPercent(item.DiscountPercentage, appliedDiscount?.Value ?? 0m)));
                calculation = InvoiceCalculationService.Calculate(
                    calculationLines,
                    isIsvExempt,
                    isTouristTaxExempt);

                for (var index = 0; index < settlementItems.Count; index++)
                    calculation.Items[index].FolioItemId = settlementItems[index].Id;
            }
            catch (InvoiceCalculationException ex)
            {
                return BadRequest(ex.Message);
            }

            decimal? cashBalanceAfter = null;
            if (settlementFolio is not null && request.PaymentMethod is "Tarjeta" or "Transferencia")
            {
                if (request.CashRegisterId.HasValue || request.CashReceived.HasValue)
                    return BadRequest("Tarjeta y transferencia no deben afectar la caja de efectivo");
                if ((request.PaymentReference?.Trim().Length ?? 0) < 3)
                    return BadRequest("Tarjeta y transferencia requieren una referencia de al menos 3 caracteres");
            }
            if (settlementFolio is not null && request.PaymentMethod == "Efectivo")
            {
                if (!request.CashRegisterId.HasValue)
                    return BadRequest("Debe seleccionar una caja abierta para registrar el pago en efectivo");
                if (!request.CashReceived.HasValue || request.CashReceived.Value < calculation.TotalAmount)
                    return BadRequest("El efectivo recibido debe cubrir el total de la factura");

                var cashLock = $"cash-register:{request.CashRegisterId.Value:N}";
                await _context.Database.ExecuteSqlInterpolatedAsync(
                    $"SELECT pg_advisory_xact_lock(hashtext({cashLock}));",
                    HttpContext.RequestAborted);
                var cashRegister = await _context.CashRegisters
                    .SingleOrDefaultAsync(register => register.Id == request.CashRegisterId.Value && register.IsActive);
                if (cashRegister is null)
                    return BadRequest("La caja seleccionada no existe o está inactiva");

                var lastMovement = await _context.CashMovements
                    .Where(movement => movement.CashRegisterId == cashRegister.Id)
                    .OrderByDescending(movement => movement.CreatedAt)
                    .ThenByDescending(movement => movement.Id)
                    .FirstOrDefaultAsync();
                if (lastMovement is null || lastMovement.MovementType == CashMovementType.Cierre)
                    return Conflict("La caja seleccionada está cerrada");
                cashBalanceAfter = TaxService.RoundCurrency(lastMovement.BalanceAfter + calculation.TotalAmount);
            }

            var invoice = new Invoice
            {
                CAIId = request.CAIId,
                DocumentAuthorizationId = authorizationResult?.AuthorizationId,
                CAINumberSnapshot = authorizationResult?.CAINumber ?? cai.CAINumber,
                AuthorizationRangeSnapshot = authorizationResult != null
                    ? $"{authorizationResult.InitialRange} - {authorizationResult.FinalRange}"
                    : $"{cai.InitialRange} - {cai.FinalRange}",
                AuthorizationDueDateSnapshot = authorizationResult?.DueDate.ToDateTime(TimeOnly.MinValue) ?? DateTime.SpecifyKind(cai.DueDate.ToDateTime(TimeOnly.MaxValue), DateTimeKind.Utc),
                CorrelativeNumber = correlative,
                CustomerId = request.CustomerId,
                GuestId = request.GuestId,
                FolioId = settlementFolio?.Id,
                RTNCliente = request.RTNCliente,
                CustomerName = request.CustomerName,
                CustomerAddress = request.CustomerAddress,
                PaymentMethod = settlementFolio is not null ? request.PaymentMethod : null,
                SubTotal = calculation.SubTotal,
                ISVAmount = calculation.ISVAmount,
                ISV15Amount = calculation.ISV15Amount,
                ISV18Amount = calculation.ISV18Amount,
                TouristTaxAmount = calculation.TouristTaxAmount,
                DiscountsAmount = calculation.DiscountsAmount,
                AppliedDiscountId = appliedDiscount?.Id,
                AppliedDiscountNameSnapshot = appliedDiscount?.Name,
                AppliedDiscountPercentageSnapshot = appliedDiscount?.Value,
                TotalAmount = calculation.TotalAmount,
                TaxableAmount = calculation.TaxableAmount,
                ExoneratedAmount = calculation.ExoneratedAmount,
                ExemptAmount = calculation.ExemptAmount,
                TaxpayerType = taxpayerType,
                ExonerationOrderNumber = request.ExonerationOrderNumber,
                SefinExonerationCertificateNumber = request.SefinExonerationCertificateNumber,
                SagRegistryNumber = request.SagRegistryNumber,
                IsIsvExempt = isIsvExempt,
                IsTouristTaxExempt = isTouristTaxExempt,
                OriginalInvoiceId = request.OriginalInvoiceId,
                OriginalCorrelativeNumber = request.OriginalInvoiceId.HasValue ? (await _repo.GetByIdAsync(request.OriginalInvoiceId.Value))?.CorrelativeNumber : null,
                Reason = request.Reason,
                DocumentType = documentType,
                Status = settlementFolio is not null ? InvoiceStatus.Pagada : InvoiceStatus.Emitida,
                InvoiceDate = HondurasTime.Now,
                CashReceived = settlementFolio is not null && request.PaymentMethod == "Efectivo"
                    ? request.CashReceived
                    : null,
                CashChange = settlementFolio is not null && request.PaymentMethod == "Efectivo"
                    ? TaxService.RoundCurrency(request.CashReceived!.Value - calculation.TotalAmount)
                    : null,
                InvoiceItems = calculation.Items.ToList()
            };

            Payment? settlementPayment = null;
            if (settlementFolio is not null)
            {
                var paymentId = Guid.CreateVersion7();
                settlementPayment = new Payment
                {
                    Id = paymentId,
                    PaymentNumber = $"PAG-{paymentId:N}".ToUpperInvariant(),
                    RecordedByUserId = userId,
                    Method = Enum.Parse<PaymentMethod>(request.PaymentMethod),
                    Currency = "HNL",
                    Amount = calculation.TotalAmount,
                    CashReceived = request.PaymentMethod == "Efectivo" ? request.CashReceived : null,
                    CashChange = request.PaymentMethod == "Efectivo"
                        ? TaxService.RoundCurrency(request.CashReceived!.Value - calculation.TotalAmount)
                        : null,
                    ExternalReference = request.PaymentMethod == "Efectivo"
                        ? string.Empty
                        : request.PaymentReference!.Trim(),
                    PaymentDate = HondurasTime.Now,
                    Status = PaymentStatus.Confirmado,
                    CashRegisterId = request.PaymentMethod == "Efectivo" ? request.CashRegisterId : null
                };
                var paymentApplication = new PaymentApplication
                {
                    Id = Guid.CreateVersion7(),
                    Payment = settlementPayment,
                    Invoice = invoice,
                    Amount = calculation.TotalAmount
                };
                settlementPayment.Applications.Add(paymentApplication);
                invoice.PaymentApplications.Add(paymentApplication);
            }

            if (settlementFolio is not null)
            {
                settlementFolio.TotalAmount = calculation.TotalAmount;
                settlementFolio.ClosingDate = HondurasTime.Now;
                settlementFolio.Status = FolioStatus.Cerrado;
                settlementFolio.Reservation.Status = ReservationStatus.CheckOut;
                settlementFolio.Reservation.Version++;
                settlementFolio.Room.Status = RoomStatus.Limpieza;
            }

            ApplyFiscalSnapshot(invoice);

            await _repo.AddAsync(invoice);
            if (settlementFolio is not null && request.PaymentMethod == "Efectivo")
            {
                _context.CashMovements.Add(new CashMovement
                {
                    CashRegisterId = request.CashRegisterId!.Value,
                    UserId = userId,
                    MovementType = CashMovementType.Ingreso,
                    Amount = calculation.TotalAmount,
                    Description = $"Cobro {settlementPayment!.PaymentNumber} - Factura {invoice.CorrelativeNumber}",
                    MovementDate = HondurasTime.Now,
                    BalanceAfter = cashBalanceAfter!.Value,
                    ReferenceId = settlementPayment!.Id
                });
            }
            await _accountingService.CreateInvoiceEntryAsync(invoice);
            if (settlementPayment is not null)
            {
                await _accountingService.CreatePaymentEntryAsync(settlementPayment);
                await _auditService.LogAsync(
                    userId,
                    "CreatePayment",
                    nameof(Payment),
                    settlementPayment.Id,
                    new
                    {
                        settlementPayment.PaymentNumber,
                        settlementPayment.Method,
                        settlementPayment.Amount,
                        InvoiceId = invoice.Id,
                        invoice.CorrelativeNumber,
                        settlementPayment.CashRegisterId,
                        settlementPayment.ExternalReference
                    },
                    invoice.CorrelativeNumber,
                    settlementPayment.Method.ToString());
            }
            await _auditService.LogAsync(userId, "CreateInvoice", nameof(Invoice), invoice.Id, new { invoice.CorrelativeNumber, invoice.TotalAmount, invoice.TaxpayerType }, invoice.CorrelativeNumber, invoice.PaymentMethod);
            if (settlementFolio is not null)
            {
                await _auditService.LogAsync(
                    userId,
                    "CheckOut",
                    nameof(Reservation),
                    settlementFolio.ReservationId,
                    new { settlementFolio.Id, InvoiceId = invoice.Id, invoice.CorrelativeNumber, settlementFolio.RoomId, settlementFolio.TotalAmount },
                    invoice.CorrelativeNumber,
                    invoice.PaymentMethod);
            }
            await _idempotencyService.StoreAsync(
                userId,
                "invoice:create",
                normalizedKey,
                requestHash,
                invoice.Id,
                HttpContext.RequestAborted);
            await transaction.CommitAsync();
            Response.Headers["Idempotency-Key"] = normalizedKey;
            return CreatedAtAction(nameof(GetById), new { id = invoice.Id }, _mapper.Map<InvoiceDto>(invoice));
        }

        [HttpPost("{id}/cancel")]
        [Authorize(Policy = PermissionNames.CreateInvoices)]
        public async Task<ActionResult> Cancel(Guid id)
        {
            var invoice = await _repo.GetByIdAsync(id);
            if (invoice == null) return NotFound();
            return BadRequest("La anulación fiscal debe realizarse mediante nota de crédito vinculada");
        }

        [HttpPost("{id}/credit-note")]
        [Authorize(Policy = PermissionNames.CreateInvoices)]
        public async Task<ActionResult<InvoiceDto>> CreateCreditNote(
            Guid id,
            [FromBody] CreateCreditNoteRequest request,
            [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey)
        {
            if (!IdempotencyService.TryNormalizeKey(idempotencyKey, out var normalizedKey))
                return InvalidIdempotencyKey();
            if (request.OriginalInvoiceId != id)
                return BadRequest("La factura original de la ruta no coincide con la solicitud");

            var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var requestHash = IdempotencyService.ComputeRequestHash(request);
            await using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                var priorAttempt = await _idempotencyService.LockAndFindAsync(
                    userId, "invoice:credit-note", normalizedKey, requestHash, HttpContext.RequestAborted);
                if (priorAttempt is not null)
                {
                    var existing = await _repo.GetByIdAsync(priorAttempt.ResourceId);
                    if (existing is null) return IdempotencyResultConflict();
                    await transaction.CommitAsync();
                    Response.Headers["Idempotency-Key"] = normalizedKey;
                    return CreatedAtAction(nameof(GetById), new { id = existing.Id }, _mapper.Map<InvoiceDto>(existing));
                }
            }
            catch (IdempotencyConflictException ex)
            {
                return IdempotencyConflict(ex.Message);
            }

            var profileBlockReason = await _fiscalProfileService.GetOperationBlockReasonAsync();
            if (profileBlockReason is not null)
                return FiscalProfileConflict(profileBlockReason);

            var adjustmentLock = $"invoice-balance:{id:N}";
            await _context.Database.ExecuteSqlInterpolatedAsync(
                $"SELECT pg_advisory_xact_lock(hashtext({adjustmentLock}));",
                HttpContext.RequestAborted);

            var original = await _context.Invoices
                .Include(invoice => invoice.InvoiceItems)
                .SingleOrDefaultAsync(invoice => invoice.Id == id);
            if (original is null) return NotFound("Factura original no encontrada");
            if (original.DocumentType != InvoiceDocumentType.Factura)
                return BadRequest("La nota de crédito debe vincularse a una factura");
            if (original.Status == InvoiceStatus.Anulada)
                return Conflict("La factura ya fue acreditada por completo");

            var requestedGroups = request.Items.GroupBy(item => item.OriginalInvoiceItemId).ToList();
            if (requestedGroups.Any(group => group.Count() > 1))
                return BadRequest("Cada línea original debe aparecer una sola vez en la nota de crédito");

            var originalLines = original.InvoiceItems.ToDictionary(item => item.Id);
            if (request.Items.Any(item => !originalLines.ContainsKey(item.OriginalInvoiceItemId)))
                return BadRequest("La nota de crédito contiene una línea que no pertenece a la factura original");

            var creditedQuantities = await _context.InvoiceItems
                .Where(item => item.OriginalInvoiceItemId.HasValue
                    && item.Invoice.OriginalInvoiceId == id
                    && item.Invoice.DocumentType == InvoiceDocumentType.NotaCredito)
                .GroupBy(item => item.OriginalInvoiceItemId!.Value)
                .Select(group => new { OriginalItemId = group.Key, Quantity = group.Sum(item => item.Quantity) })
                .ToDictionaryAsync(item => item.OriginalItemId, item => item.Quantity);

            foreach (var requested in request.Items)
            {
                var originalLine = originalLines[requested.OriginalInvoiceItemId];
                var remaining = originalLine.Quantity - creditedQuantities.GetValueOrDefault(originalLine.Id);
                if (requested.Quantity > remaining)
                    return Conflict($"La cantidad acreditada para '{originalLine.Description}' excede el saldo disponible");
            }

            InvoiceCalculation calculation;
            try
            {
                calculation = InvoiceCalculationService.Calculate(
                    request.Items.Select(requested =>
                    {
                        var source = originalLines[requested.OriginalInvoiceItemId];
                        return new InvoiceLineInput(
                            source.Description,
                            requested.Quantity,
                            source.UnitPrice,
                            source.IsExempt,
                            source.ISVRate,
                            source.IsTouristTaxable,
                            source.DiscountPercentage,
                            source.Id);
                    }),
                    original.IsIsvExempt,
                    original.IsTouristTaxExempt);
            }
            catch (InvoiceCalculationException ex)
            {
                return BadRequest(ex.Message);
            }

            var caiValidation = await ValidateActiveCaiAsync(request.CAIId);
            if (caiValidation is not null) return caiValidation;

            FiscalCorrelativeResult authResult;
            try
            {
                authResult = await _fiscalAuthorizationService.GetNextCorrelativeAsync(
                    InvoiceDocumentType.NotaCredito,
                    request.DocumentAuthorizationId);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }

            var creditNote = new Invoice
            {
                CAIId = request.CAIId,
                DocumentAuthorizationId = authResult.AuthorizationId,
                CAINumberSnapshot = authResult.CAINumber,
                AuthorizationRangeSnapshot = $"{authResult.InitialRange} - {authResult.FinalRange}",
                AuthorizationDueDateSnapshot = DateTime.SpecifyKind(authResult.DueDate.ToDateTime(TimeOnly.MaxValue), DateTimeKind.Utc),
                CorrelativeNumber = authResult.CorrelativeNumber,
                OriginalInvoiceId = original.Id,
                OriginalCorrelativeNumber = original.CorrelativeNumber,
                Reason = request.Reason.Trim(),
                CustomerId = original.CustomerId,
                GuestId = original.GuestId ?? request.GuestId,
                RTNCliente = original.RTNCliente,
                CustomerName = original.CustomerName,
                CustomerAddress = original.CustomerAddress,
                SubTotal = calculation.SubTotal,
                ISVAmount = calculation.ISVAmount,
                ISV15Amount = calculation.ISV15Amount,
                ISV18Amount = calculation.ISV18Amount,
                TouristTaxAmount = calculation.TouristTaxAmount,
                DiscountsAmount = calculation.DiscountsAmount,
                TotalAmount = calculation.TotalAmount,
                TaxableAmount = calculation.TaxableAmount,
                ExemptAmount = calculation.ExemptAmount,
                ExoneratedAmount = calculation.ExoneratedAmount,
                TaxpayerType = original.TaxpayerType,
                ExonerationOrderNumber = original.ExonerationOrderNumber,
                SefinExonerationCertificateNumber = original.SefinExonerationCertificateNumber,
                SagRegistryNumber = original.SagRegistryNumber,
                IsIsvExempt = original.IsIsvExempt,
                IsTouristTaxExempt = original.IsTouristTaxExempt,
                DocumentType = InvoiceDocumentType.NotaCredito,
                Status = InvoiceStatus.Emitida,
                InvoiceDate = HondurasTime.Now,
                InvoiceItems = calculation.Items.ToList()
            };

            var requestedByLine = request.Items.ToDictionary(item => item.OriginalInvoiceItemId, item => item.Quantity);
            var fullyCredited = original.InvoiceItems.All(line =>
                creditedQuantities.GetValueOrDefault(line.Id) + requestedByLine.GetValueOrDefault(line.Id) == line.Quantity);
            if (fullyCredited)
                original.Status = InvoiceStatus.Anulada;

            ApplyFiscalSnapshot(creditNote);

            await _repo.AddAsync(creditNote);
            await _accountingService.CreateInvoiceEntryAsync(creditNote);
            await _auditService.LogAsync(userId, "CreateCreditNote", nameof(Invoice), creditNote.Id, new { creditNote.CorrelativeNumber, Original = original.CorrelativeNumber, creditNote.TotalAmount, Reason = creditNote.Reason }, creditNote.CorrelativeNumber);
            await _idempotencyService.StoreAsync(userId, "invoice:credit-note", normalizedKey, requestHash, creditNote.Id, HttpContext.RequestAborted);
            await transaction.CommitAsync();
            Response.Headers["Idempotency-Key"] = normalizedKey;
            return CreatedAtAction(nameof(GetById), new { id = creditNote.Id }, _mapper.Map<InvoiceDto>(creditNote));
        }

        [HttpPost("{id}/debit-note")]
        [Authorize(Policy = PermissionNames.CreateInvoices)]
        public async Task<ActionResult<InvoiceDto>> CreateDebitNote(
            Guid id,
            [FromBody] CreateDebitNoteRequest request,
            [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey)
        {
            if (!IdempotencyService.TryNormalizeKey(idempotencyKey, out var normalizedKey))
                return InvalidIdempotencyKey();
            if (request.OriginalInvoiceId != id)
                return BadRequest("La factura original de la ruta no coincide con la solicitud");

            var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var requestHash = IdempotencyService.ComputeRequestHash(request);
            await using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                var priorAttempt = await _idempotencyService.LockAndFindAsync(
                    userId, "invoice:debit-note", normalizedKey, requestHash, HttpContext.RequestAborted);
                if (priorAttempt is not null)
                {
                    var existing = await _repo.GetByIdAsync(priorAttempt.ResourceId);
                    if (existing is null) return IdempotencyResultConflict();
                    await transaction.CommitAsync();
                    Response.Headers["Idempotency-Key"] = normalizedKey;
                    return CreatedAtAction(nameof(GetById), new { id = existing.Id }, _mapper.Map<InvoiceDto>(existing));
                }
            }
            catch (IdempotencyConflictException ex)
            {
                return IdempotencyConflict(ex.Message);
            }

            var profileBlockReason = await _fiscalProfileService.GetOperationBlockReasonAsync();
            if (profileBlockReason is not null)
                return FiscalProfileConflict(profileBlockReason);

            var adjustmentLock = $"invoice-adjust:{id:N}";
            await _context.Database.ExecuteSqlInterpolatedAsync(
                $"SELECT pg_advisory_xact_lock(hashtext({adjustmentLock}));",
                HttpContext.RequestAborted);

            var original = await _context.Invoices.SingleOrDefaultAsync(invoice => invoice.Id == id);
            if (original is null) return NotFound("Factura original no encontrada");
            if (original.DocumentType != InvoiceDocumentType.Factura)
                return BadRequest("La nota de débito debe vincularse a una factura");
            if (original.Status == InvoiceStatus.Anulada)
                return Conflict("No se puede cargar una factura acreditada por completo");

            InvoiceCalculation calculation;
            try
            {
                calculation = InvoiceCalculationService.Calculate(
                    request.Items.Select(ToCalculationLine),
                    original.IsIsvExempt,
                    original.IsTouristTaxExempt);
            }
            catch (InvoiceCalculationException ex)
            {
                return BadRequest(ex.Message);
            }

            var caiValidation = await ValidateActiveCaiAsync(request.CAIId);
            if (caiValidation is not null) return caiValidation;

            FiscalCorrelativeResult authResult;
            try
            {
                authResult = await _fiscalAuthorizationService.GetNextCorrelativeAsync(
                    InvoiceDocumentType.NotaDebito,
                    request.DocumentAuthorizationId);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }

            var debitNote = new Invoice
            {
                CAIId = request.CAIId,
                DocumentAuthorizationId = authResult.AuthorizationId,
                CAINumberSnapshot = authResult.CAINumber,
                AuthorizationRangeSnapshot = $"{authResult.InitialRange} - {authResult.FinalRange}",
                AuthorizationDueDateSnapshot = DateTime.SpecifyKind(authResult.DueDate.ToDateTime(TimeOnly.MaxValue), DateTimeKind.Utc),
                CorrelativeNumber = authResult.CorrelativeNumber,
                OriginalInvoiceId = original.Id,
                OriginalCorrelativeNumber = original.CorrelativeNumber,
                Reason = request.Reason.Trim(),
                CustomerId = original.CustomerId,
                GuestId = original.GuestId ?? request.GuestId,
                RTNCliente = original.RTNCliente,
                CustomerName = original.CustomerName,
                CustomerAddress = original.CustomerAddress,
                SubTotal = calculation.SubTotal,
                ISVAmount = calculation.ISVAmount,
                ISV15Amount = calculation.ISV15Amount,
                ISV18Amount = calculation.ISV18Amount,
                TouristTaxAmount = calculation.TouristTaxAmount,
                DiscountsAmount = calculation.DiscountsAmount,
                TotalAmount = calculation.TotalAmount,
                TaxableAmount = calculation.TaxableAmount,
                ExemptAmount = calculation.ExemptAmount,
                ExoneratedAmount = calculation.ExoneratedAmount,
                TaxpayerType = original.TaxpayerType,
                ExonerationOrderNumber = original.ExonerationOrderNumber,
                SefinExonerationCertificateNumber = original.SefinExonerationCertificateNumber,
                SagRegistryNumber = original.SagRegistryNumber,
                IsIsvExempt = original.IsIsvExempt,
                IsTouristTaxExempt = original.IsTouristTaxExempt,
                DocumentType = InvoiceDocumentType.NotaDebito,
                Status = InvoiceStatus.Emitida,
                InvoiceDate = HondurasTime.Now,
                InvoiceItems = calculation.Items.ToList()
            };

            ApplyFiscalSnapshot(debitNote);

            await _repo.AddAsync(debitNote);
            await _accountingService.CreateInvoiceEntryAsync(debitNote);
            await _auditService.LogAsync(userId, "CreateDebitNote", nameof(Invoice), debitNote.Id, new { debitNote.CorrelativeNumber, Original = original.CorrelativeNumber, debitNote.TotalAmount, Reason = debitNote.Reason }, debitNote.CorrelativeNumber);
            await _idempotencyService.StoreAsync(userId, "invoice:debit-note", normalizedKey, requestHash, debitNote.Id, HttpContext.RequestAborted);
            await transaction.CommitAsync();
            Response.Headers["Idempotency-Key"] = normalizedKey;
            return CreatedAtAction(nameof(GetById), new { id = debitNote.Id }, _mapper.Map<InvoiceDto>(debitNote));
        }

        private static InvoiceLineInput ToCalculationLine(InvoiceItemDto item)
            => new(
                item.Description,
                item.Quantity,
                item.UnitPrice,
                item.IsExempt,
                item.ISVRate,
                item.IsTouristTaxable,
                item.DiscountPercentage,
                item.OriginalInvoiceItemId);

        private async Task<ActionResult?> ValidateActiveCaiAsync(Guid caiId)
        {
            var cai = await _caiRepo.GetByIdAsync(caiId);
            if (cai is null) return BadRequest("CAI no encontrado");
            if (cai.Status != CAIStatus.Activo) return BadRequest("El CAI no está activo");
            if (cai.DueDate < HondurasTime.Today) return BadRequest("El CAI está vencido");
            return null;
        }

        private BadRequestObjectResult InvalidIdempotencyKey()
            => BadRequest(new ProblemDetails
            {
                Type = "https://httpstatuses.com/400",
                Title = "Clave de idempotencia inválida",
                Status = StatusCodes.Status400BadRequest,
                Detail = "La cabecera Idempotency-Key es obligatoria y debe contener un UUID válido."
            });

        private ConflictObjectResult IdempotencyConflict(string detail)
            => Conflict(new ProblemDetails
            {
                Type = "https://httpstatuses.com/409",
                Title = "Conflicto de idempotencia",
                Status = StatusCodes.Status409Conflict,
                Detail = detail
            });

        private ConflictObjectResult IdempotencyResultConflict()
            => Conflict(new ProblemDetails
            {
                Type = "https://httpstatuses.com/409",
                Title = "Resultado idempotente inconsistente",
                Status = StatusCodes.Status409Conflict,
                Detail = "La operación ya fue registrada, pero su documento no está disponible."
            });

        private static void ApplyFiscalSnapshot(Invoice invoice)
        {
            var snapshot = new
            {
                invoice.CAIId,
                invoice.DocumentAuthorizationId,
                invoice.CAINumberSnapshot,
                invoice.AuthorizationRangeSnapshot,
                invoice.AuthorizationDueDateSnapshot,
                invoice.CorrelativeNumber,
                invoice.InvoiceDate,
                invoice.CustomerName,
                invoice.RTNCliente,
                invoice.CustomerAddress,
                invoice.FolioId,
                invoice.DocumentType,
                invoice.Status,
                invoice.TaxpayerType,
                invoice.SubTotal,
                invoice.ISVAmount,
                invoice.TouristTaxAmount,
                invoice.DiscountsAmount,
                invoice.AppliedDiscountId,
                invoice.AppliedDiscountNameSnapshot,
                invoice.AppliedDiscountPercentageSnapshot,
                invoice.TotalAmount,
                invoice.TaxableAmount,
                invoice.ExemptAmount,
                invoice.ExoneratedAmount,
                invoice.IsIsvExempt,
                invoice.IsTouristTaxExempt,
                invoice.ExonerationOrderNumber,
                invoice.SefinExonerationCertificateNumber,
                invoice.SagRegistryNumber,
                invoice.OriginalInvoiceId,
                invoice.OriginalCorrelativeNumber,
                invoice.Reason,
                invoice.PaymentMethod,
                invoice.CashReceived,
                invoice.CashChange,
                Items = invoice.InvoiceItems.Select(item => new
                {
                    item.OriginalInvoiceItemId,
                    item.FolioItemId,
                    item.Description,
                    item.Quantity,
                    item.UnitPrice,
                    item.LineTotal,
                    item.IsExempt,
                    item.ISVRate,
                    item.IsTouristTaxable,
                    item.DiscountPercentage
                }).ToList()
            };

            invoice.FiscalSnapshotJson = JsonSerializer.Serialize(snapshot, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
            invoice.FiscalHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(invoice.FiscalSnapshotJson)));
        }

        private static decimal CombineDiscountPercent(decimal lineDiscount, decimal settlementDiscount)
        {
            var remainingPercent = (100m - lineDiscount) * (100m - settlementDiscount) / 100m;
            return Math.Round(100m - remainingPercent, 6, MidpointRounding.AwayFromZero);
        }

        private ConflictObjectResult FiscalProfileConflict(string detail)
            => Conflict(new ProblemDetails
            {
                Type = "https://httpstatuses.com/409",
                Title = "Perfil fiscal no habilitado",
                Status = StatusCodes.Status409Conflict,
                Detail = detail
            });
    }
}
