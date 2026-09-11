using System.Security.Claims;
using AutoMapper;
using hotel_erp.Api.Authorization;
using hotel_erp.Api.Database;
using hotel_erp.Api.Database.Entities;
using hotel_erp.Api.Dtos.Payments;
using hotel_erp.Api.Services;
using hotel_erp.Api.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace hotel_erp.Api.Controllers
{
    [ApiController]
    [Route("api/payments")]
    [Authorize]
    public class PaymentsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IAccountingService _accountingService;
        private readonly AuditService _auditService;
        private readonly IdempotencyService _idempotencyService;
        private readonly IMapper _mapper;

        public PaymentsController(
            ApplicationDbContext context,
            IAccountingService accountingService,
            AuditService auditService,
            IdempotencyService idempotencyService,
            IMapper mapper)
        {
            _context = context;
            _accountingService = accountingService;
            _auditService = auditService;
            _idempotencyService = idempotencyService;
            _mapper = mapper;
        }

        [HttpGet]
        [Authorize(Policy = PermissionNames.ManageCash)]
        public async Task<ActionResult<IEnumerable<PaymentDto>>> GetAll([FromQuery] Guid? invoiceId)
        {
            var query = DetailedPayments().OrderByDescending(payment => payment.PaymentDate).AsQueryable();
            if (invoiceId.HasValue)
                query = query.Where(payment => payment.Applications.Any(application => application.InvoiceId == invoiceId.Value));
            return Ok(_mapper.Map<IEnumerable<PaymentDto>>(await query.AsSplitQuery().ToListAsync()));
        }

        [HttpGet("{id}")]
        [Authorize(Policy = PermissionNames.ManageCash)]
        public async Task<ActionResult<PaymentDto>> GetById(Guid id)
        {
            var payment = await DetailedPayments().AsSplitQuery().SingleOrDefaultAsync(candidate => candidate.Id == id);
            return payment is null ? NotFound() : Ok(_mapper.Map<PaymentDto>(payment));
        }

        [HttpPost]
        [Authorize(Policy = PermissionNames.ManageCash)]
        public async Task<ActionResult<PaymentDto>> Create(
            [FromBody] CreatePaymentRequest request,
            [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey)
        {
            if (!IdempotencyService.TryNormalizeKey(idempotencyKey, out var normalizedKey))
                return BadRequest("Idempotency-Key debe ser un UUID válido");
            if (!Enum.TryParse<PaymentMethod>(request.Method, out var method))
                return BadRequest("Método de pago no soportado");

            var amount = TaxService.RoundCurrency(request.Amount);
            var applications = request.Applications
                .Select(application => new
                {
                    application.InvoiceId,
                    Amount = TaxService.RoundCurrency(application.Amount)
                })
                .OrderBy(application => application.InvoiceId)
                .ToList();
            if (applications.Select(application => application.InvoiceId).Distinct().Count() != applications.Count)
                return BadRequest("Una factura solo puede aparecer una vez dentro del pago");
            if (applications.Sum(application => application.Amount) != amount)
                return BadRequest("La suma de aplicaciones debe coincidir exactamente con el importe del pago");

            var cashReceived = request.CashReceived.HasValue
                ? TaxService.RoundCurrency(request.CashReceived.Value)
                : (decimal?)null;
            var externalReference = request.ExternalReference?.Trim() ?? string.Empty;
            if (method == PaymentMethod.Efectivo)
            {
                if (!request.CashRegisterId.HasValue)
                    return BadRequest("El pago en efectivo requiere una caja abierta");
                if (!cashReceived.HasValue || cashReceived.Value < amount)
                    return BadRequest("El efectivo recibido debe cubrir el importe aplicado");
            }
            else
            {
                if (request.CashRegisterId.HasValue || cashReceived.HasValue)
                    return BadRequest("Tarjeta y transferencia no deben afectar la caja de efectivo");
                if (externalReference.Length < 3)
                    return BadRequest("Tarjeta y transferencia requieren una referencia de al menos 3 caracteres");
            }

            var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var requestHash = IdempotencyService.ComputeRequestHash(new
            {
                Method = method.ToString(),
                Currency = request.Currency,
                Amount = amount,
                CashReceived = cashReceived,
                request.CashRegisterId,
                ExternalReference = externalReference,
                Applications = applications
            });
            await using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var existingIntent = await _idempotencyService.LockAndFindAsync(
                    userId,
                    "payment:create",
                    normalizedKey,
                    requestHash,
                    HttpContext.RequestAborted);
                if (existingIntent is not null)
                {
                    var existingPayment = await DetailedPayments()
                        .AsSplitQuery()
                        .SingleOrDefaultAsync(payment => payment.Id == existingIntent.ResourceId);
                    if (existingPayment is null)
                        return Conflict("El resultado idempotente del pago ya no está disponible");
                    await transaction.CommitAsync();
                    return CreatedAtAction(nameof(GetById), new { id = existingPayment.Id }, _mapper.Map<PaymentDto>(existingPayment));
                }
            }
            catch (IdempotencyConflictException exception)
            {
                return Conflict(exception.Message);
            }

            foreach (var invoiceId in applications.Select(application => application.InvoiceId))
            {
                var lockName = $"invoice-balance:{invoiceId:N}";
                await _context.Database.ExecuteSqlInterpolatedAsync(
                    $"SELECT pg_advisory_xact_lock(hashtext({lockName}));",
                    HttpContext.RequestAborted);
            }

            var invoiceIds = applications.Select(application => application.InvoiceId).ToList();
            var invoices = await _context.Invoices
                .Include(invoice => invoice.PaymentApplications)
                .ThenInclude(application => application.Payment)
                .Include(invoice => invoice.CreditNotes)
                .Include(invoice => invoice.Folio)
                .ThenInclude(folio => folio!.Reservation)
                .Include(invoice => invoice.Folio)
                .ThenInclude(folio => folio!.Room)
                .Where(invoice => invoiceIds.Contains(invoice.Id))
                .AsSplitQuery()
                .ToListAsync();
            if (invoices.Count != invoiceIds.Count)
                return BadRequest("Una o más facturas no existen");

            foreach (var application in applications)
            {
                var invoice = invoices.Single(candidate => candidate.Id == application.InvoiceId);
                if (invoice.DocumentType == InvoiceDocumentType.NotaCredito || invoice.Status == InvoiceStatus.Anulada)
                    return Conflict($"El documento {invoice.CorrelativeNumber} no admite pagos");
                var paid = invoice.PaymentApplications
                    .Where(existing => existing.Payment.Status != PaymentStatus.Anulado)
                    .Sum(existing => existing.Amount);
                var credited = invoice.CreditNotes
                    .Where(note => note.DocumentType == InvoiceDocumentType.NotaCredito)
                    .Sum(note => note.TotalAmount);
                if (application.Amount > TaxService.RoundCurrency(invoice.TotalAmount - paid - credited))
                    return Conflict($"El pago excede el saldo de {invoice.CorrelativeNumber}");
            }

            decimal? cashBalanceAfter = null;
            if (method == PaymentMethod.Efectivo)
            {
                var cashRegisterId = request.CashRegisterId!.Value;
                var cashLock = $"cash-register:{cashRegisterId:N}";
                await _context.Database.ExecuteSqlInterpolatedAsync(
                    $"SELECT pg_advisory_xact_lock(hashtext({cashLock}));",
                    HttpContext.RequestAborted);
                var register = await _context.CashRegisters
                    .SingleOrDefaultAsync(candidate => candidate.Id == cashRegisterId && candidate.IsActive);
                if (register is null)
                    return BadRequest("La caja seleccionada no existe o está inactiva");
                var lastMovement = await _context.CashMovements
                    .Where(movement => movement.CashRegisterId == cashRegisterId)
                    .OrderByDescending(movement => movement.CreatedAt)
                    .ThenByDescending(movement => movement.Id)
                    .FirstOrDefaultAsync();
                if (lastMovement is null || lastMovement.MovementType == CashMovementType.Cierre)
                    return Conflict("La caja seleccionada está cerrada");
                cashBalanceAfter = TaxService.RoundCurrency(lastMovement.BalanceAfter + amount);
            }

            var now = HondurasTime.Now;
            var paymentId = Guid.CreateVersion7();
            var recordedBy = await _context.Users.SingleAsync(user => user.Id == userId);
            var payment = new Payment
            {
                Id = paymentId,
                PaymentNumber = $"PAG-{paymentId:N}".ToUpperInvariant(),
                RecordedByUserId = userId,
                RecordedByUser = recordedBy,
                Method = method,
                Currency = request.Currency,
                Amount = amount,
                CashReceived = cashReceived,
                CashChange = method == PaymentMethod.Efectivo
                    ? TaxService.RoundCurrency(cashReceived!.Value - amount)
                    : null,
                ExternalReference = externalReference,
                PaymentDate = now,
                Status = PaymentStatus.Confirmado,
                CashRegisterId = request.CashRegisterId,
                Applications = applications.Select(application => new PaymentApplication
                {
                    Id = Guid.CreateVersion7(),
                    InvoiceId = application.InvoiceId,
                    Invoice = invoices.Single(invoice => invoice.Id == application.InvoiceId),
                    Amount = application.Amount
                }).ToList()
            };
            _context.Payments.Add(payment);

            if (method == PaymentMethod.Efectivo)
            {
                _context.CashMovements.Add(new CashMovement
                {
                    Id = Guid.CreateVersion7(),
                    CashRegisterId = request.CashRegisterId!.Value,
                    UserId = userId,
                    MovementType = CashMovementType.Ingreso,
                    Amount = amount,
                    Description = $"Cobro {payment.PaymentNumber}",
                    MovementDate = now,
                    BalanceAfter = cashBalanceAfter!.Value,
                    ReferenceId = payment.Id
                });
            }

            foreach (var invoice in invoices)
            {
                var newApplication = applications.Single(application => application.InvoiceId == invoice.Id);
                var priorPaid = invoice.PaymentApplications
                    .Where(existing => existing.PaymentId != payment.Id && existing.Payment.Status != PaymentStatus.Anulado)
                    .Sum(existing => existing.Amount);
                var credited = invoice.CreditNotes
                    .Where(note => note.DocumentType == InvoiceDocumentType.NotaCredito)
                    .Sum(note => note.TotalAmount);
                var paidAfter = TaxService.RoundCurrency(priorPaid + newApplication.Amount);
                invoice.Status = paidAfter + credited == invoice.TotalAmount
                    ? InvoiceStatus.Pagada
                    : InvoiceStatus.Emitida;
                var methods = invoice.PaymentApplications
                    .Where(existing => existing.PaymentId != payment.Id && existing.Payment.Status != PaymentStatus.Anulado)
                    .Select(existing => existing.Payment.Method)
                    .Append(method)
                    .Distinct()
                    .ToList();
                var paymentMethodSummary = methods.Count == 1 ? methods[0].ToString() : "Mixto";

                if (invoice.Status == InvoiceStatus.Pagada && invoice.Folio?.Status == FolioStatus.PendientePago)
                {
                    invoice.Folio.Status = FolioStatus.Cerrado;
                    invoice.Folio.ClosingDate = now;
                    invoice.Folio.Reservation.Status = ReservationStatus.CheckOut;
                    invoice.Folio.Reservation.Version++;
                    invoice.Folio.Room.Status = RoomStatus.Limpieza;
                    await _auditService.LogAsync(
                        userId,
                        "CheckOut",
                        nameof(Reservation),
                        invoice.Folio.ReservationId,
                        new { invoice.FolioId, InvoiceId = invoice.Id, PaymentId = payment.Id, invoice.CorrelativeNumber },
                        invoice.CorrelativeNumber,
                        paymentMethodSummary);
                }
            }

            await _context.SaveChangesAsync();
            await _accountingService.CreatePaymentEntryAsync(payment);
            await _auditService.LogAsync(
                userId,
                "CreatePayment",
                nameof(Payment),
                payment.Id,
                new
                {
                    payment.PaymentNumber,
                    payment.Method,
                    payment.Currency,
                    payment.Amount,
                    payment.CashRegisterId,
                    payment.ExternalReference,
                    Applications = applications
                },
                paymentMethod: payment.Method.ToString());
            await _idempotencyService.StoreAsync(
                userId,
                "payment:create",
                normalizedKey,
                requestHash,
                payment.Id,
                HttpContext.RequestAborted);
            await transaction.CommitAsync();
            return CreatedAtAction(nameof(GetById), new { id = payment.Id }, _mapper.Map<PaymentDto>(payment));
        }

        private IQueryable<Payment> DetailedPayments() => _context.Payments
            .Include(payment => payment.RecordedByUser)
            .Include(payment => payment.CashRegister)
            .Include(payment => payment.AccountingEntry)
            .Include(payment => payment.Applications)
            .ThenInclude(application => application.Invoice)
            .Include(payment => payment.Refunds)
            .ThenInclude(refund => refund.RecordedByUser)
            .Include(payment => payment.Refunds)
            .ThenInclude(refund => refund.CashRegister)
            .Include(payment => payment.Refunds)
            .ThenInclude(refund => refund.Applications)
            .ThenInclude(application => application.CreditNote)
            .Include(payment => payment.CardSettlementApplications)
            .ThenInclude(application => application.CardSettlement);
    }
}
