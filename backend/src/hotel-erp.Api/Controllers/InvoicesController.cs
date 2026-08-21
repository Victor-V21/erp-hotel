using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using AutoMapper;
using hotel_erp.Api.Dtos.Common;
using hotel_erp.Api.Services.Interfaces;
using hotel_erp.Api.Services;
using hotel_erp.Api.Database.Entities;
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
        private readonly AuditService _auditService;
        private readonly IMapper _mapper;

        public InvoicesController(
            IInvoiceRepository repo,
            ICAIRepository caiRepo,
            ICustomerRepository customerRepo,
            IAccountingService accountingService,
            IFiscalAuthorizationService fiscalAuthorizationService,
            AuditService auditService,
            IMapper mapper)
        {
            _repo = repo;
            _caiRepo = caiRepo;
            _customerRepo = customerRepo;
            _accountingService = accountingService;
            _fiscalAuthorizationService = fiscalAuthorizationService;
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
        public async Task<ActionResult<IEnumerable<InvoiceDto>>> Search([FromQuery] string? dni, [FromQuery] DateTime? start, [FromQuery] DateTime? end, [FromQuery] Guid? caiId, [FromQuery] Guid? documentAuthorizationId)
        {
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
        public async Task<ActionResult<InvoiceDto>> Update(Guid id, [FromBody] CreateInvoiceRequest request)
        {
            var invoice = await _repo.GetByIdAsync(id);
            if (invoice == null) return NotFound();
            if (invoice.DocumentType == InvoiceDocumentType.Factura)
                return BadRequest("Una factura fiscal emitida no puede modificarse; use nota de crédito o débito");
            if (invoice.Status == InvoiceStatus.Anulada)
                return BadRequest("No se puede modificar una factura anulada");

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
            return Ok(_mapper.Map<InvoiceDto>(invoice));
        }

        [HttpPost]
        public async Task<ActionResult<InvoiceDto>> Create([FromBody] CreateInvoiceRequest request)
        {
            var cai = await _caiRepo.GetByIdAsync(request.CAIId);
            if (cai == null) return BadRequest("CAI no encontrado");
            if (cai.Status != CAIStatus.Activo) return BadRequest("El CAI no está activo");
            if (cai.DueDate < HondurasTime.Today) return BadRequest("El CAI está vencido");

            var documentType = Enum.Parse<InvoiceDocumentType>(request.DocumentType);
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

            // Calculate taxes
            decimal subtotal = 0;
            decimal isvAmount = 0;
            decimal touristTax = 0;
            decimal discounts = 0;

            var items = request.Items.Select(item =>
            {
                var lineTotal = item.Quantity * item.UnitPrice;
                var discount = lineTotal * (item.DiscountPercentage / 100m);
                var lineAfterDiscount = lineTotal - discount;
                subtotal += lineAfterDiscount;
                if (!item.IsExempt && !isIsvExempt) isvAmount += lineAfterDiscount * item.ISVRate;
                if (item.IsTouristTaxable && !isTouristTaxExempt) touristTax += lineAfterDiscount * 0.04m;
                discounts += discount;

                return new InvoiceItem
                {
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
                RTNCliente = request.RTNCliente,
                CustomerName = request.CustomerName,
                CustomerAddress = request.CustomerAddress,
                SubTotal = TaxService.RoundCurrency(subtotal),
                ISVAmount = TaxService.RoundCurrency(isvAmount),
                ISV15Amount = TaxService.RoundCurrency(isvAmount),
                ISV18Amount = 0,
                TouristTaxAmount = TaxService.RoundCurrency(touristTax),
                DiscountsAmount = TaxService.RoundCurrency(discounts),
                TotalAmount = TaxService.RoundCurrency(subtotal + isvAmount + touristTax),
                TaxableAmount = isIsvExempt ? 0 : TaxService.RoundCurrency(subtotal),
                ExoneratedAmount = isIsvExempt ? TaxService.RoundCurrency(subtotal) : 0,
                ExemptAmount = 0,
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
                Status = InvoiceStatus.Emitida,
                InvoiceItems = items
            };

            ApplyFiscalSnapshot(invoice);

            await _repo.AddAsync(invoice);
            await _accountingService.CreateInvoiceEntryAsync(invoice);
            var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            await _auditService.LogAsync(userId, "CreateInvoice", nameof(Invoice), invoice.Id, new { invoice.CorrelativeNumber, invoice.TotalAmount, invoice.TaxpayerType }, invoice.CorrelativeNumber, invoice.PaymentMethod);
            return CreatedAtAction(nameof(GetById), new { id = invoice.Id }, _mapper.Map<InvoiceDto>(invoice));
        }

        [HttpPost("{id}/cancel")]
        public async Task<ActionResult> Cancel(Guid id)
        {
            var invoice = await _repo.GetByIdAsync(id);
            if (invoice == null) return NotFound();
            return BadRequest("La anulación fiscal debe realizarse mediante nota de crédito vinculada");
        }

        [HttpPost("{id}/credit-note")]
        public async Task<ActionResult<InvoiceDto>> CreateCreditNote(Guid id, [FromBody] CreateCreditNoteRequest request)
        {
            var original = await _repo.GetByIdAsync(id);
            if (original == null) return NotFound("Factura original no encontrada");
            if (original.DocumentType != InvoiceDocumentType.Factura && original.DocumentType != InvoiceDocumentType.NotaDebito)
                return BadRequest("Solo se puede emitir nota de crédito sobre una factura o nota de débito");
            if (original.Status == InvoiceStatus.Anulada)
                return BadRequest("La factura original está anulada");
            if (string.IsNullOrWhiteSpace(request.Reason))
                return BadRequest("La nota de crédito requiere una razón detallada");

            var cai = await _caiRepo.GetByIdAsync(request.CAIId);
            if (cai == null) return BadRequest("CAI no encontrado");
            if (cai.Status != CAIStatus.Activo) return BadRequest("El CAI no está activo");

            var documentType = InvoiceDocumentType.NotaCredito;
            FiscalCorrelativeResult? authResult = null;
            var correlative = string.Empty;
            try
            {
                if (request.DocumentAuthorizationId.HasValue)
                {
                    authResult = await _fiscalAuthorizationService.GetNextCorrelativeAsync(documentType, request.DocumentAuthorizationId);
                    correlative = authResult.CorrelativeNumber;
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

            original.Status = InvoiceStatus.Anulada;
            await _repo.UpdateAsync(original);

            decimal subtotal = 0;
            var items = request.Items.Select(item =>
            {
                var lineTotal = item.Quantity * item.UnitPrice;
                var discount = lineTotal * (item.DiscountPercentage / 100m);
                subtotal += lineTotal - discount;
                return new InvoiceItem
                {
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

            var isvAmount = request.Items.Sum(i => i.IsExempt ? 0 : (i.LineTotal - i.LineTotal * (i.DiscountPercentage / 100m)) * i.ISVRate);
            var touristTax = request.Items.Sum(i => i.IsTouristTaxable ? (i.LineTotal - i.LineTotal * (i.DiscountPercentage / 100m)) * 0.04m : 0);

            var creditNote = new Invoice
            {
                CAIId = request.CAIId,
                DocumentAuthorizationId = authResult?.AuthorizationId,
                CAINumberSnapshot = authResult?.CAINumber ?? cai.CAINumber,
                AuthorizationRangeSnapshot = authResult != null
                    ? $"{authResult.InitialRange} - {authResult.FinalRange}"
                    : $"{cai.InitialRange} - {cai.FinalRange}",
                AuthorizationDueDateSnapshot = authResult?.DueDate.ToDateTime(TimeOnly.MinValue) ?? DateTime.SpecifyKind(cai.DueDate.ToDateTime(TimeOnly.MaxValue), DateTimeKind.Utc),
                CorrelativeNumber = correlative,
                OriginalInvoiceId = original.Id,
                OriginalCorrelativeNumber = original.CorrelativeNumber,
                Reason = request.Reason,
                CustomerId = original.CustomerId,
                GuestId = original.GuestId ?? request.GuestId,
                RTNCliente = original.RTNCliente,
                CustomerName = original.CustomerName,
                CustomerAddress = original.CustomerAddress,
                SubTotal = TaxService.RoundCurrency(subtotal),
                ISVAmount = TaxService.RoundCurrency(isvAmount),
                ISV15Amount = TaxService.RoundCurrency(isvAmount),
                ISV18Amount = 0,
                TouristTaxAmount = TaxService.RoundCurrency(touristTax),
                TotalAmount = TaxService.RoundCurrency(subtotal + isvAmount + touristTax),
                TaxpayerType = original.TaxpayerType,
                DocumentType = documentType,
                Status = InvoiceStatus.Emitida,
                InvoiceDate = HondurasTime.Now,
                InvoiceItems = items
            };

            ApplyFiscalSnapshot(creditNote);

            await _repo.AddAsync(creditNote);
            await _accountingService.CreateInvoiceEntryAsync(creditNote);
            var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            await _auditService.LogAsync(userId, "CreateCreditNote", nameof(Invoice), creditNote.Id, new { creditNote.CorrelativeNumber, Original = original.CorrelativeNumber, Reason = request.Reason }, creditNote.CorrelativeNumber);
            return CreatedAtAction(nameof(GetById), new { id = creditNote.Id }, _mapper.Map<InvoiceDto>(creditNote));
        }

        [HttpPost("{id}/debit-note")]
        public async Task<ActionResult<InvoiceDto>> CreateDebitNote(Guid id, [FromBody] CreateDebitNoteRequest request)
        {
            var original = await _repo.GetByIdAsync(id);
            if (original == null) return NotFound("Factura original no encontrada");
            if (original.DocumentType != InvoiceDocumentType.Factura)
                return BadRequest("Solo se puede emitir nota de débito sobre una factura");
            if (original.Status == InvoiceStatus.Anulada)
                return BadRequest("La factura original está anulada");
            if (string.IsNullOrWhiteSpace(request.Reason))
                return BadRequest("La nota de débito requiere una razón detallada");

            var cai = await _caiRepo.GetByIdAsync(request.CAIId);
            if (cai == null) return BadRequest("CAI no encontrado");
            if (cai.Status != CAIStatus.Activo) return BadRequest("El CAI no está activo");

            var documentType = InvoiceDocumentType.NotaDebito;
            FiscalCorrelativeResult? authResult = null;
            var correlative = string.Empty;
            try
            {
                if (request.DocumentAuthorizationId.HasValue)
                {
                    authResult = await _fiscalAuthorizationService.GetNextCorrelativeAsync(documentType, request.DocumentAuthorizationId);
                    correlative = authResult.CorrelativeNumber;
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

            decimal subtotal = 0;
            var items = request.Items.Select(item =>
            {
                var lineTotal = item.Quantity * item.UnitPrice;
                var discount = lineTotal * (item.DiscountPercentage / 100m);
                subtotal += lineTotal - discount;
                return new InvoiceItem
                {
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

            var isvAmount = request.Items.Sum(i => i.IsExempt ? 0 : (i.LineTotal - i.LineTotal * (i.DiscountPercentage / 100m)) * i.ISVRate);
            var touristTax = request.Items.Sum(i => i.IsTouristTaxable ? (i.LineTotal - i.LineTotal * (i.DiscountPercentage / 100m)) * 0.04m : 0);

            var debitNote = new Invoice
            {
                CAIId = request.CAIId,
                DocumentAuthorizationId = authResult?.AuthorizationId,
                CAINumberSnapshot = authResult?.CAINumber ?? cai.CAINumber,
                AuthorizationRangeSnapshot = authResult != null
                    ? $"{authResult.InitialRange} - {authResult.FinalRange}"
                    : $"{cai.InitialRange} - {cai.FinalRange}",
                AuthorizationDueDateSnapshot = authResult?.DueDate.ToDateTime(TimeOnly.MinValue) ?? DateTime.SpecifyKind(cai.DueDate.ToDateTime(TimeOnly.MaxValue), DateTimeKind.Utc),
                CorrelativeNumber = correlative,
                OriginalInvoiceId = original.Id,
                OriginalCorrelativeNumber = original.CorrelativeNumber,
                Reason = request.Reason,
                CustomerId = original.CustomerId,
                GuestId = original.GuestId ?? request.GuestId,
                RTNCliente = original.RTNCliente,
                CustomerName = original.CustomerName,
                CustomerAddress = original.CustomerAddress,
                SubTotal = TaxService.RoundCurrency(subtotal),
                ISVAmount = TaxService.RoundCurrency(isvAmount),
                ISV15Amount = TaxService.RoundCurrency(isvAmount),
                ISV18Amount = 0,
                TouristTaxAmount = TaxService.RoundCurrency(touristTax),
                TotalAmount = TaxService.RoundCurrency(subtotal + isvAmount + touristTax),
                TaxpayerType = original.TaxpayerType,
                DocumentType = documentType,
                Status = InvoiceStatus.Emitida,
                InvoiceDate = HondurasTime.Now,
                InvoiceItems = items
            };

            ApplyFiscalSnapshot(debitNote);

            await _repo.AddAsync(debitNote);
            await _accountingService.CreateInvoiceEntryAsync(debitNote);
            var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            await _auditService.LogAsync(userId, "CreateDebitNote", nameof(Invoice), debitNote.Id, new { debitNote.CorrelativeNumber, Original = original.CorrelativeNumber, Reason = request.Reason }, debitNote.CorrelativeNumber);
            return CreatedAtAction(nameof(GetById), new { id = debitNote.Id }, _mapper.Map<InvoiceDto>(debitNote));
        }

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
                invoice.DocumentType,
                invoice.Status,
                invoice.TaxpayerType,
                invoice.SubTotal,
                invoice.ISVAmount,
                invoice.TouristTaxAmount,
                invoice.DiscountsAmount,
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
                Items = invoice.InvoiceItems.Select(item => new
                {
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
    }
}

