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
    [Route("api/payments/{paymentId:guid}/refunds")]
    [Authorize(Policy = PermissionNames.ManageCash)]
    public class RefundsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IAccountingService _accountingService;
        private readonly AuditService _auditService;
        private readonly IdempotencyService _idempotencyService;
        private readonly IMapper _mapper;

        public RefundsController(
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
        public async Task<ActionResult<IEnumerable<RefundDto>>> GetAll(Guid paymentId)
        {
            var exists = await _context.Payments.AnyAsync(payment => payment.Id == paymentId);
            if (!exists) return NotFound();
            var refunds = await DetailedRefunds(paymentId)
                .OrderByDescending(refund => refund.RefundDate)
                .AsSplitQuery()
                .ToListAsync();
            return Ok(_mapper.Map<IEnumerable<RefundDto>>(refunds));
        }

        [HttpGet("{id:guid}")]
        public async Task<ActionResult<RefundDto>> GetById(Guid paymentId, Guid id)
        {
            var refund = await DetailedRefunds(paymentId)
                .AsSplitQuery()
                .SingleOrDefaultAsync(candidate => candidate.Id == id);
            return refund is null ? NotFound() : Ok(_mapper.Map<RefundDto>(refund));
        }

        [HttpPost]
        public async Task<ActionResult<RefundDto>> Create(
            Guid paymentId,
            [FromBody] CreateRefundRequest request,
            [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey)
        {
            if (!IdempotencyService.TryNormalizeKey(idempotencyKey, out var normalizedKey))
                return BadRequest("Idempotency-Key debe ser un UUID válido");

            var amount = TaxService.RoundCurrency(request.Amount);
            var applications = request.Applications
                .Select(application => new
                {
                    application.CreditNoteId,
                    Amount = TaxService.RoundCurrency(application.Amount)
                })
                .OrderBy(application => application.CreditNoteId)
                .ToList();
            if (applications.Select(application => application.CreditNoteId).Distinct().Count() != applications.Count)
                return BadRequest("Una nota de crédito solo puede aparecer una vez dentro del reembolso");
            if (applications.Sum(application => application.Amount) != amount)
                return BadRequest("La suma de aplicaciones debe coincidir exactamente con el importe del reembolso");

            var reason = request.Reason.Trim();
            if (reason.Length < 3)
                return BadRequest("El motivo del reembolso debe contener al menos 3 caracteres");
            var externalReference = request.ExternalReference?.Trim() ?? string.Empty;
            var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var requestHash = IdempotencyService.ComputeRequestHash(new
            {
                PaymentId = paymentId,
                Amount = amount,
                request.CashRegisterId,
                ExternalReference = externalReference,
                Reason = reason,
                Applications = applications
            });

            await using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var existingIntent = await _idempotencyService.LockAndFindAsync(
                    userId,
                    "payment:refund",
                    normalizedKey,
                    requestHash,
                    HttpContext.RequestAborted);
                if (existingIntent is not null)
                {
                    var existingRefund = await DetailedRefunds(paymentId)
                        .AsSplitQuery()
                        .SingleOrDefaultAsync(refund => refund.Id == existingIntent.ResourceId);
                    if (existingRefund is null)
                        return Conflict("El resultado idempotente del reembolso ya no está disponible");
                    await transaction.CommitAsync();
                    return CreatedAtAction(
                        nameof(GetById),
                        new { paymentId, id = existingRefund.Id },
                        _mapper.Map<RefundDto>(existingRefund));
                }
            }
            catch (IdempotencyConflictException exception)
            {
                return Conflict(exception.Message);
            }

            var paymentLock = $"payment-financial:{paymentId:N}";
            await _context.Database.ExecuteSqlInterpolatedAsync(
                $"SELECT pg_advisory_xact_lock(hashtext({paymentLock}));",
                HttpContext.RequestAborted);

            var requestedCreditNoteIds = applications.Select(application => application.CreditNoteId).ToList();
            var noteOrigins = await _context.Invoices
                .Where(invoice => requestedCreditNoteIds.Contains(invoice.Id))
                .Select(invoice => new { invoice.Id, invoice.OriginalInvoiceId })
                .ToListAsync();
            if (noteOrigins.Count != requestedCreditNoteIds.Count || noteOrigins.Any(note => !note.OriginalInvoiceId.HasValue))
                return BadRequest("Una o más notas de crédito no existen o no tienen documento original");

            var invoiceIds = noteOrigins
                .Select(note => note.OriginalInvoiceId!.Value)
                .Distinct()
                .OrderBy(id => id)
                .ToList();
            foreach (var invoiceId in invoiceIds)
            {
                var balanceLock = $"invoice-balance:{invoiceId:N}";
                await _context.Database.ExecuteSqlInterpolatedAsync(
                    $"SELECT pg_advisory_xact_lock(hashtext({balanceLock}));",
                    HttpContext.RequestAborted);
            }

            var payment = await _context.Payments
                .Include(candidate => candidate.Applications)
                .Include(candidate => candidate.Refunds)
                .Include(candidate => candidate.CardSettlementApplications)
                .ThenInclude(application => application.CardSettlement)
                .AsSplitQuery()
                .SingleOrDefaultAsync(candidate => candidate.Id == paymentId);
            if (payment is null) return NotFound("Pago no encontrado");
            if (payment.Status == PaymentStatus.Anulado)
                return Conflict("El pago anulado no admite reembolsos");

            var previouslyRefundedFromPayment = payment.Refunds
                .Where(refund => refund.Status == RefundStatus.Confirmado)
                .Sum(refund => refund.Amount);
            if (amount > TaxService.RoundCurrency(payment.Amount - previouslyRefundedFromPayment))
                return Conflict("El reembolso excede el importe disponible del pago original");
            if (payment.Method == PaymentMethod.Tarjeta)
            {
                var settledFromPayment = payment.CardSettlementApplications
                    .Where(application => application.CardSettlement.Status == CardSettlementStatus.Confirmado)
                    .Sum(application => application.Amount);
                var unsettledAmount = TaxService.RoundCurrency(
                    payment.Amount - previouslyRefundedFromPayment - settledFromPayment);
                if (amount > unsettledAmount)
                    return Conflict("El pago con tarjeta ya fue liquidado; su devolución requiere el flujo de reversión del adquirente");
            }

            var creditNotes = await _context.Invoices
                .Where(invoice => requestedCreditNoteIds.Contains(invoice.Id))
                .ToListAsync();
            var paymentInvoiceIds = payment.Applications.Select(application => application.InvoiceId).ToHashSet();
            foreach (var application in applications)
            {
                var creditNote = creditNotes.Single(note => note.Id == application.CreditNoteId);
                if (creditNote.DocumentType != InvoiceDocumentType.NotaCredito
                    || !creditNote.OriginalInvoiceId.HasValue
                    || !paymentInvoiceIds.Contains(creditNote.OriginalInvoiceId.Value))
                {
                    return Conflict($"El documento {creditNote.CorrelativeNumber} no respalda un reembolso de este pago");
                }

                var alreadyAppliedToNote = await _context.RefundApplications
                    .Where(existing => existing.CreditNoteId == creditNote.Id
                        && existing.Refund.Status == RefundStatus.Confirmado)
                    .SumAsync(existing => existing.Amount);
                if (application.Amount > TaxService.RoundCurrency(creditNote.TotalAmount - alreadyAppliedToNote))
                    return Conflict($"El reembolso excede el saldo de la nota {creditNote.CorrelativeNumber}");
            }

            foreach (var invoiceId in invoiceIds)
            {
                var invoiceTotal = await _context.Invoices
                    .Where(invoice => invoice.Id == invoiceId)
                    .Select(invoice => invoice.TotalAmount)
                    .SingleAsync();
                var paid = await _context.PaymentApplications
                    .Where(application => application.InvoiceId == invoiceId
                        && application.Payment.Status != PaymentStatus.Anulado)
                    .SumAsync(application => application.Amount);
                var credited = await _context.Invoices
                    .Where(invoice => invoice.OriginalInvoiceId == invoiceId
                        && invoice.DocumentType == InvoiceDocumentType.NotaCredito)
                    .SumAsync(invoice => invoice.TotalAmount);
                var refunded = await _context.RefundApplications
                    .Where(application => application.CreditNote.OriginalInvoiceId == invoiceId
                        && application.Refund.Status == RefundStatus.Confirmado)
                    .SumAsync(application => application.Amount);
                var requestedForInvoice = applications
                    .Where(application => noteOrigins.Single(note => note.Id == application.CreditNoteId).OriginalInvoiceId == invoiceId)
                    .Sum(application => application.Amount);
                var availableCustomerCredit = Math.Max(
                    0m,
                    TaxService.RoundCurrency(paid + credited - invoiceTotal - refunded));
                if (requestedForInvoice > availableCustomerCredit)
                    return Conflict("El reembolso excede el saldo a favor respaldado por pagos y notas de crédito");
            }

            CashRegister? cashRegister = null;
            decimal? cashBalanceAfter = null;
            if (payment.Method == PaymentMethod.Efectivo)
            {
                if (!request.CashRegisterId.HasValue)
                    return BadRequest("El reembolso en efectivo requiere una caja abierta");
                if (!string.IsNullOrEmpty(externalReference))
                    return BadRequest("El reembolso en efectivo no utiliza referencia bancaria");

                var cashRegisterId = request.CashRegisterId.Value;
                var cashLock = $"cash-register:{cashRegisterId:N}";
                await _context.Database.ExecuteSqlInterpolatedAsync(
                    $"SELECT pg_advisory_xact_lock(hashtext({cashLock}));",
                    HttpContext.RequestAborted);
                cashRegister = await _context.CashRegisters
                    .SingleOrDefaultAsync(register => register.Id == cashRegisterId && register.IsActive);
                if (cashRegister is null)
                    return BadRequest("La caja seleccionada no existe o está inactiva");
                var lastMovement = await _context.CashMovements
                    .Where(movement => movement.CashRegisterId == cashRegisterId)
                    .OrderByDescending(movement => movement.CreatedAt)
                    .ThenByDescending(movement => movement.Id)
                    .FirstOrDefaultAsync();
                if (lastMovement is null || lastMovement.MovementType == CashMovementType.Cierre)
                    return Conflict("La caja seleccionada está cerrada");
                if (lastMovement.BalanceAfter < amount)
                    return Conflict("La caja no tiene saldo suficiente para efectuar el reembolso");
                cashBalanceAfter = TaxService.RoundCurrency(lastMovement.BalanceAfter - amount);
            }
            else
            {
                if (request.CashRegisterId.HasValue)
                    return BadRequest("Tarjeta y transferencia no deben afectar la caja de efectivo");
                if (externalReference.Length < 3)
                    return BadRequest("La devolución por tarjeta o transferencia requiere una referencia de al menos 3 caracteres");
            }

            var now = HondurasTime.Now;
            var refundId = Guid.CreateVersion7();
            var recordedBy = await _context.Users.SingleAsync(user => user.Id == userId);
            var refund = new Refund
            {
                Id = refundId,
                RefundNumber = $"REF-{refundId:N}".ToUpperInvariant(),
                PaymentId = payment.Id,
                Payment = payment,
                RecordedByUserId = userId,
                RecordedByUser = recordedBy,
                Method = payment.Method,
                Currency = "HNL",
                Amount = amount,
                ExternalReference = payment.Method == PaymentMethod.Efectivo ? string.Empty : externalReference,
                Reason = reason,
                RefundDate = now,
                Status = RefundStatus.Confirmado,
                CashRegisterId = payment.Method == PaymentMethod.Efectivo ? request.CashRegisterId : null,
                CashRegister = cashRegister,
                Applications = applications.Select(application => new RefundApplication
                {
                    Id = Guid.CreateVersion7(),
                    CreditNoteId = application.CreditNoteId,
                    CreditNote = creditNotes.Single(note => note.Id == application.CreditNoteId),
                    Amount = application.Amount
                }).ToList()
            };
            _context.Refunds.Add(refund);

            if (payment.Method == PaymentMethod.Efectivo)
            {
                _context.CashMovements.Add(new CashMovement
                {
                    Id = Guid.CreateVersion7(),
                    CashRegisterId = request.CashRegisterId!.Value,
                    UserId = userId,
                    MovementType = CashMovementType.Egreso,
                    Amount = amount,
                    Description = $"Reembolso {refund.RefundNumber}",
                    MovementDate = now,
                    BalanceAfter = cashBalanceAfter!.Value,
                    ReferenceId = refund.Id
                });
            }

            var refundedAfter = TaxService.RoundCurrency(previouslyRefundedFromPayment + amount);
            payment.Status = refundedAfter == payment.Amount
                ? PaymentStatus.Reembolsado
                : PaymentStatus.ParcialmenteReembolsado;

            await _context.SaveChangesAsync();
            await _accountingService.CreateRefundEntryAsync(refund);
            await _auditService.LogAsync(
                userId,
                "CreateRefund",
                nameof(Refund),
                refund.Id,
                new
                {
                    refund.RefundNumber,
                    refund.PaymentId,
                    payment.PaymentNumber,
                    refund.Method,
                    refund.Amount,
                    refund.CashRegisterId,
                    refund.ExternalReference,
                    refund.Reason,
                    Applications = applications
                },
                paymentMethod: refund.Method.ToString());
            await _idempotencyService.StoreAsync(
                userId,
                "payment:refund",
                normalizedKey,
                requestHash,
                refund.Id,
                HttpContext.RequestAborted);
            await transaction.CommitAsync();
            return CreatedAtAction(
                nameof(GetById),
                new { paymentId, id = refund.Id },
                _mapper.Map<RefundDto>(refund));
        }

        private IQueryable<Refund> DetailedRefunds(Guid paymentId) => _context.Refunds
            .Where(refund => refund.PaymentId == paymentId)
            .Include(refund => refund.Payment)
            .Include(refund => refund.RecordedByUser)
            .Include(refund => refund.CashRegister)
            .Include(refund => refund.AccountingEntry)
            .Include(refund => refund.Applications)
            .ThenInclude(application => application.CreditNote);
    }
}
