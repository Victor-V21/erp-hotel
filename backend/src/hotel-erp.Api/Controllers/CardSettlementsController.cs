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
    [Route("api/card-settlements")]
    [Authorize(Policy = PermissionNames.ManageAccounting)]
    public class CardSettlementsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IAccountingService _accountingService;
        private readonly AuditService _auditService;
        private readonly IdempotencyService _idempotencyService;
        private readonly IMapper _mapper;

        public CardSettlementsController(
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
        public async Task<ActionResult<IEnumerable<CardSettlementDto>>> GetAll()
        {
            var settlements = await DetailedSettlements()
                .OrderByDescending(settlement => settlement.SettlementDate)
                .ThenByDescending(settlement => settlement.CreatedAt)
                .AsSplitQuery()
                .ToListAsync();
            return Ok(_mapper.Map<IEnumerable<CardSettlementDto>>(settlements));
        }

        [HttpGet("{id:guid}")]
        public async Task<ActionResult<CardSettlementDto>> GetById(Guid id)
        {
            var settlement = await DetailedSettlements()
                .AsSplitQuery()
                .SingleOrDefaultAsync(candidate => candidate.Id == id);
            return settlement is null
                ? NotFound()
                : Ok(_mapper.Map<CardSettlementDto>(settlement));
        }

        [HttpGet("eligible-payments")]
        public async Task<ActionResult<IEnumerable<EligibleCardPaymentDto>>> GetEligiblePayments()
        {
            var payments = await _context.Payments
                .Where(payment => payment.Method == PaymentMethod.Tarjeta
                    && payment.Status != PaymentStatus.Anulado)
                .OrderBy(payment => payment.PaymentDate)
                .Select(payment => new
                {
                    payment.Id,
                    payment.PaymentNumber,
                    payment.PaymentDate,
                    payment.ExternalReference,
                    payment.Amount,
                    RefundedAmount = payment.Refunds
                        .Where(refund => refund.Status == RefundStatus.Confirmado)
                        .Sum(refund => (decimal?)refund.Amount) ?? 0m,
                    SettledAmount = payment.CardSettlementApplications
                        .Where(application => application.CardSettlement.Status == CardSettlementStatus.Confirmado)
                        .Sum(application => (decimal?)application.Amount) ?? 0m
                })
                .ToListAsync();

            var eligible = payments
                .Select(payment =>
                {
                    return new EligibleCardPaymentDto
                    {
                        Id = payment.Id,
                        PaymentNumber = payment.PaymentNumber,
                        PaymentDate = payment.PaymentDate,
                        ExternalReference = payment.ExternalReference,
                        Amount = payment.Amount,
                        RefundedAmount = payment.RefundedAmount,
                        SettledAmount = payment.SettledAmount,
                        AvailableAmount = Math.Max(
                            0m,
                            TaxService.RoundCurrency(
                                payment.Amount - payment.RefundedAmount - payment.SettledAmount))
                    };
                })
                .Where(payment => payment.AvailableAmount > 0m)
                .ToList();
            return Ok(eligible);
        }

        [HttpPost]
        public async Task<ActionResult<CardSettlementDto>> Create(
            [FromBody] CreateCardSettlementRequest request,
            [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey)
        {
            if (!IdempotencyService.TryNormalizeKey(idempotencyKey, out var normalizedKey))
                return BadRequest("Idempotency-Key debe ser un UUID válido");
            if (request.SettlementDate == default)
                return BadRequest("La fecha de liquidación es obligatoria");
            if (request.SettlementDate > HondurasTime.Today)
                return BadRequest("La fecha de liquidación no puede estar en el futuro");

            var grossAmount = TaxService.RoundCurrency(request.GrossAmount);
            var bankDepositAmount = TaxService.RoundCurrency(request.BankDepositAmount);
            var commissionAmount = TaxService.RoundCurrency(request.CommissionAmount);
            var withholdingAmount = TaxService.RoundCurrency(request.WithholdingAmount);
            var applications = request.Applications
                .Select(application => new
                {
                    application.PaymentId,
                    Amount = TaxService.RoundCurrency(application.Amount)
                })
                .OrderBy(application => application.PaymentId)
                .ToList();
            if (grossAmount <= 0m || applications.Any(application => application.Amount <= 0m))
                return BadRequest("Los importes de la liquidación deben ser mayores que cero");
            if (applications.Select(application => application.PaymentId).Distinct().Count() != applications.Count)
                return BadRequest("Un pago solo puede aparecer una vez dentro de la liquidación");
            if (applications.Sum(application => application.Amount) != grossAmount)
                return BadRequest("La suma de pagos aplicados debe coincidir exactamente con el importe bruto");
            if (bankDepositAmount + commissionAmount + withholdingAmount != grossAmount)
                return BadRequest("Depósito, comisión y retención deben sumar exactamente el importe bruto");

            var externalReference = request.ExternalReference.Trim().ToUpperInvariant();
            if (externalReference.Length < 3)
                return BadRequest("La liquidación requiere una referencia de al menos 3 caracteres");

            var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var requestHash = IdempotencyService.ComputeRequestHash(new
            {
                Currency = request.Currency,
                GrossAmount = grossAmount,
                BankDepositAmount = bankDepositAmount,
                CommissionAmount = commissionAmount,
                WithholdingAmount = withholdingAmount,
                ExternalReference = externalReference,
                request.SettlementDate,
                Applications = applications
            });

            await using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var existingIntent = await _idempotencyService.LockAndFindAsync(
                    userId,
                    "card-settlement:create",
                    normalizedKey,
                    requestHash,
                    HttpContext.RequestAborted);
                if (existingIntent is not null)
                {
                    var existingSettlement = await DetailedSettlements()
                        .AsSplitQuery()
                        .SingleOrDefaultAsync(settlement => settlement.Id == existingIntent.ResourceId);
                    if (existingSettlement is null)
                        return Conflict("El resultado idempotente de la liquidación ya no está disponible");
                    await transaction.CommitAsync();
                    return CreatedAtAction(
                        nameof(GetById),
                        new { id = existingSettlement.Id },
                        _mapper.Map<CardSettlementDto>(existingSettlement));
                }
            }
            catch (IdempotencyConflictException exception)
            {
                return Conflict(exception.Message);
            }

            var referenceLock = $"card-settlement-reference:{externalReference.ToUpperInvariant()}";
            await _context.Database.ExecuteSqlInterpolatedAsync(
                $"SELECT pg_advisory_xact_lock(hashtext({referenceLock}));",
                HttpContext.RequestAborted);
            if (await _context.CardSettlements.AnyAsync(
                    settlement => settlement.ExternalReference == externalReference,
                    HttpContext.RequestAborted))
            {
                return Conflict("La referencia del adquirente ya fue registrada");
            }

            foreach (var paymentId in applications.Select(application => application.PaymentId))
            {
                var paymentLock = $"payment-financial:{paymentId:N}";
                await _context.Database.ExecuteSqlInterpolatedAsync(
                    $"SELECT pg_advisory_xact_lock(hashtext({paymentLock}));",
                    HttpContext.RequestAborted);
            }

            var paymentIds = applications.Select(application => application.PaymentId).ToList();
            var payments = await _context.Payments
                .Include(payment => payment.Refunds)
                .Include(payment => payment.CardSettlementApplications)
                .ThenInclude(application => application.CardSettlement)
                .Where(payment => paymentIds.Contains(payment.Id))
                .AsSplitQuery()
                .ToListAsync();
            if (payments.Count != paymentIds.Count)
                return BadRequest("Uno o más pagos no existen");

            foreach (var application in applications)
            {
                var payment = payments.Single(candidate => candidate.Id == application.PaymentId);
                if (payment.Method != PaymentMethod.Tarjeta || payment.Status == PaymentStatus.Anulado)
                    return Conflict($"El pago {payment.PaymentNumber} no es un cobro con tarjeta liquidable");
                var refunded = payment.Refunds
                    .Where(refund => refund.Status == RefundStatus.Confirmado)
                    .Sum(refund => refund.Amount);
                var settled = payment.CardSettlementApplications
                    .Where(existing => existing.CardSettlement.Status == CardSettlementStatus.Confirmado)
                    .Sum(existing => existing.Amount);
                var available = Math.Max(0m, TaxService.RoundCurrency(payment.Amount - refunded - settled));
                if (application.Amount > available)
                    return Conflict($"La aplicación excede el saldo por liquidar de {payment.PaymentNumber}");
            }

            var settlementId = Guid.CreateVersion7();
            var recordedBy = await _context.Users.SingleAsync(user => user.Id == userId);
            var settlement = new CardSettlement
            {
                Id = settlementId,
                SettlementNumber = $"LTJ-{settlementId:N}".ToUpperInvariant(),
                RecordedByUserId = userId,
                RecordedByUser = recordedBy,
                Currency = request.Currency,
                GrossAmount = grossAmount,
                BankDepositAmount = bankDepositAmount,
                CommissionAmount = commissionAmount,
                WithholdingAmount = withholdingAmount,
                ExternalReference = externalReference,
                SettlementDate = request.SettlementDate,
                Status = CardSettlementStatus.Confirmado,
                Applications = applications.Select(application => new CardSettlementApplication
                {
                    Id = Guid.CreateVersion7(),
                    PaymentId = application.PaymentId,
                    Payment = payments.Single(payment => payment.Id == application.PaymentId),
                    Amount = application.Amount
                }).ToList()
            };
            _context.CardSettlements.Add(settlement);
            await _context.SaveChangesAsync();
            await _accountingService.CreateCardSettlementEntryAsync(settlement);
            await _auditService.LogAsync(
                userId,
                "CreateCardSettlement",
                nameof(CardSettlement),
                settlement.Id,
                new
                {
                    settlement.SettlementNumber,
                    settlement.Currency,
                    settlement.GrossAmount,
                    settlement.BankDepositAmount,
                    settlement.CommissionAmount,
                    settlement.WithholdingAmount,
                    settlement.ExternalReference,
                    settlement.SettlementDate,
                    Applications = applications
                });
            await _idempotencyService.StoreAsync(
                userId,
                "card-settlement:create",
                normalizedKey,
                requestHash,
                settlement.Id,
                HttpContext.RequestAborted);
            await transaction.CommitAsync();
            return CreatedAtAction(
                nameof(GetById),
                new { id = settlement.Id },
                _mapper.Map<CardSettlementDto>(settlement));
        }

        private IQueryable<CardSettlement> DetailedSettlements() => _context.CardSettlements
            .Include(settlement => settlement.RecordedByUser)
            .Include(settlement => settlement.AccountingEntry)
            .Include(settlement => settlement.Applications)
            .ThenInclude(application => application.Payment);
    }
}
