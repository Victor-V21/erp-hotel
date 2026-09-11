using AutoMapper;
using hotel_erp.Api.Authorization;
using hotel_erp.Api.Database;
using hotel_erp.Api.Dtos.Cash;
using hotel_erp.Api.Services;
using hotel_erp.Api.Database.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace hotel_erp.Api.Controllers
{
    [ApiController]
    [Route("api/cash-registers")]
    [Authorize]
    public class CashRegistersController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly AuditService _auditService;
        private readonly IdempotencyService _idempotencyService;
        private readonly IMapper _mapper;

        public CashRegistersController(
            ApplicationDbContext context,
            AuditService auditService,
            IdempotencyService idempotencyService,
            IMapper mapper)
        {
            _context = context;
            _auditService = auditService;
            _idempotencyService = idempotencyService;
            _mapper = mapper;
        }

        [HttpGet]
        [Authorize(Policy = PermissionNames.ManageCash)]
        public async Task<ActionResult<IEnumerable<CashRegisterDto>>> GetAll()
        {
            var registers = await _context.CashRegisters
                .AsNoTracking()
                .Include(register => register.CashMovements
                    .OrderByDescending(movement => movement.CreatedAt)
                    .ThenByDescending(movement => movement.Id)
                    .Take(1))
                .OrderBy(register => register.Name)
                .ToListAsync();

            return Ok(registers.Select(register => ToDto(register, register.CashMovements.SingleOrDefault())));
        }

        [HttpPost]
        [Authorize(Policy = PermissionNames.ManageCash)]
        public async Task<ActionResult<CashRegisterDto>> Create([FromBody] CreateCashRegisterRequest request)
        {
            await using var transaction = await _context.Database.BeginTransactionAsync();
            const string catalogLock = "cash-register-catalog";
            await _context.Database.ExecuteSqlInterpolatedAsync(
                $"SELECT pg_advisory_xact_lock(hashtext({catalogLock}));");

            var name = request.Name.Trim();
            if (await _context.CashRegisters.AnyAsync(register => register.Name.ToLower() == name.ToLower()))
                return Conflict("Ya existe una caja con ese nombre");

            var entity = new CashRegister
            {
                Name = name,
                Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim()
            };
            _context.CashRegisters.Add(entity);
            await _context.SaveChangesAsync();
            var userId = CurrentUserId();
            await _auditService.LogAsync(
                userId,
                "CreateCashRegister",
                nameof(CashRegister),
                entity.Id,
                new { entity.Name, entity.Description });
            await transaction.CommitAsync();
            return CreatedAtAction(nameof(GetAll), new { id = entity.Id }, ToDto(entity, null));
        }

        [HttpPost("{id}/open")]
        [Authorize(Policy = PermissionNames.ManageCash)]
        public async Task<ActionResult> Open(
            Guid id,
            [FromBody] OpenCashRegisterRequest request,
            [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey)
        {
            if (!IdempotencyService.TryNormalizeKey(idempotencyKey, out var normalizedKey))
                return BadRequest("Idempotency-Key debe ser un UUID válido");

            var userId = CurrentUserId();
            var initialAmount = TaxService.RoundCurrency(request.InitialAmount);
            var requestHash = IdempotencyService.ComputeRequestHash(new { CashRegisterId = id, InitialAmount = initialAmount });
            await using var transaction = await _context.Database.BeginTransactionAsync();
            IdempotencyRecord? existingIntent;
            try
            {
                existingIntent = await _idempotencyService.LockAndFindAsync(
                    userId,
                    "cash-register-open",
                    normalizedKey,
                    requestHash,
                    HttpContext.RequestAborted);
            }
            catch (IdempotencyConflictException ex)
            {
                return Conflict(ex.Message);
            }

            if (existingIntent is not null)
            {
                var existingMovement = await _context.CashMovements
                    .SingleOrDefaultAsync(movement => movement.Id == existingIntent.ResourceId);
                if (existingMovement is null || existingMovement.MovementType != CashMovementType.Apertura)
                    return Conflict("La operación idempotente no conserva su movimiento de apertura");
                await transaction.CommitAsync();
                return Ok(OpenResult(existingMovement));
            }

            await LockCashRegisterAsync(id);
            var register = await _context.CashRegisters.SingleOrDefaultAsync(candidate => candidate.Id == id);
            if (register == null) return NotFound();
            if (!register.IsActive) return Conflict("La caja está inactiva");

            var lastMovement = await LastMovementAsync(id);
            if (lastMovement != null && lastMovement.MovementType != CashMovementType.Cierre)
                return Conflict("La caja ya se encuentra abierta");

            var movement = new CashMovement
            {
                CashRegisterId = id,
                UserId = userId,
                MovementType = CashMovementType.Apertura,
                Amount = initialAmount,
                Description = "Apertura de caja",
                MovementDate = HondurasTime.Now,
                BalanceAfter = initialAmount
            };
            _context.CashMovements.Add(movement);
            await _context.SaveChangesAsync();
            await _auditService.LogAsync(
                userId,
                "OpenCashRegister",
                nameof(CashRegister),
                register.Id,
                new { MovementId = movement.Id, InitialAmount = initialAmount });
            await _idempotencyService.StoreAsync(
                userId,
                "cash-register-open",
                normalizedKey,
                requestHash,
                movement.Id,
                HttpContext.RequestAborted);
            await transaction.CommitAsync();
            return Ok(OpenResult(movement));
        }

        [HttpPost("{id}/close")]
        [Authorize(Policy = PermissionNames.ManageCash)]
        public async Task<ActionResult> Close(
            Guid id,
            [FromBody] CloseCashRegisterRequest request,
            [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey)
        {
            if (!IdempotencyService.TryNormalizeKey(idempotencyKey, out var normalizedKey))
                return BadRequest("Idempotency-Key debe ser un UUID válido");

            var userId = CurrentUserId();
            var countedAmount = TaxService.RoundCurrency(request.CountedAmount);
            var notes = request.Notes?.Trim();
            var requestHash = IdempotencyService.ComputeRequestHash(new { CashRegisterId = id, CountedAmount = countedAmount, Notes = notes });
            await using var transaction = await _context.Database.BeginTransactionAsync();
            IdempotencyRecord? existingIntent;
            try
            {
                existingIntent = await _idempotencyService.LockAndFindAsync(
                    userId,
                    "cash-register-close",
                    normalizedKey,
                    requestHash,
                    HttpContext.RequestAborted);
            }
            catch (IdempotencyConflictException ex)
            {
                return Conflict(ex.Message);
            }

            if (existingIntent is not null)
            {
                var existingMovement = await _context.CashMovements
                    .SingleOrDefaultAsync(movement => movement.Id == existingIntent.ResourceId);
                if (existingMovement is null || existingMovement.MovementType != CashMovementType.Cierre)
                    return Conflict("La operación idempotente no conserva su movimiento de cierre");
                await transaction.CommitAsync();
                return Ok(CloseResult(existingMovement));
            }

            await LockCashRegisterAsync(id);
            var register = await _context.CashRegisters.SingleOrDefaultAsync(candidate => candidate.Id == id);
            if (register == null) return NotFound();
            if (!register.IsActive) return Conflict("La caja está inactiva");

            var lastMovement = await LastMovementAsync(id);
            if (lastMovement == null || lastMovement.MovementType == CashMovementType.Cierre)
                return Conflict("La caja ya se encuentra cerrada");

            var expectedAmount = TaxService.RoundCurrency(lastMovement.BalanceAfter);
            var difference = TaxService.RoundCurrency(countedAmount - expectedAmount);
            if (difference != 0m && (notes?.Length ?? 0) < 3)
                return BadRequest("Debe explicar la diferencia de caja con al menos 3 caracteres");

            var movement = new CashMovement
            {
                CashRegisterId = id,
                UserId = userId,
                MovementType = CashMovementType.Cierre,
                Amount = 0m,
                Description = difference == 0m
                    ? "Cierre de caja sin diferencia"
                    : $"Cierre de caja. Diferencia: L {difference:0.00}",
                MovementDate = HondurasTime.Now,
                BalanceAfter = 0m,
                ExpectedAmount = expectedAmount,
                CountedAmount = countedAmount,
                Difference = difference,
                Notes = notes
            };
            _context.CashMovements.Add(movement);
            await _context.SaveChangesAsync();
            await _auditService.LogAsync(
                userId,
                "CloseCashRegister",
                nameof(CashRegister),
                register.Id,
                new { MovementId = movement.Id, expectedAmount, countedAmount, difference, Notes = notes });
            await _idempotencyService.StoreAsync(
                userId,
                "cash-register-close",
                normalizedKey,
                requestHash,
                movement.Id,
                HttpContext.RequestAborted);
            await transaction.CommitAsync();
            return Ok(CloseResult(movement));
        }

        [HttpGet("{id}/movements")]
        [Authorize(Policy = PermissionNames.ManageCash)]
        public async Task<ActionResult<IEnumerable<CashMovementDto>>> GetMovements(Guid id)
        {
            if (!await _context.CashRegisters.AnyAsync(register => register.Id == id))
                return NotFound();

            var movements = await _context.CashMovements
                .AsNoTracking()
                .Include(movement => movement.CashRegister)
                .Include(movement => movement.User)
                .Where(movement => movement.CashRegisterId == id)
                .OrderByDescending(movement => movement.CreatedAt)
                .ThenByDescending(movement => movement.Id)
                .ToListAsync();
            return Ok(_mapper.Map<IEnumerable<CashMovementDto>>(movements));
        }

        private Guid CurrentUserId() => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        private async Task LockCashRegisterAsync(Guid id)
        {
            var lockName = $"cash-register:{id:N}";
            await _context.Database.ExecuteSqlInterpolatedAsync(
                $"SELECT pg_advisory_xact_lock(hashtext({lockName}));",
                HttpContext.RequestAborted);
        }

        private Task<CashMovement?> LastMovementAsync(Guid id) => _context.CashMovements
            .Where(movement => movement.CashRegisterId == id)
            .OrderByDescending(movement => movement.CreatedAt)
            .ThenByDescending(movement => movement.Id)
            .FirstOrDefaultAsync();

        private static CashRegisterDto ToDto(CashRegister register, CashMovement? latest) => new()
        {
            Id = register.Id,
            Name = register.Name,
            Description = register.Description,
            IsActive = register.IsActive,
            IsOpen = latest is not null && latest.MovementType != CashMovementType.Cierre,
            CurrentBalance = latest is not null && latest.MovementType != CashMovementType.Cierre
                ? latest.BalanceAfter
                : 0m,
            LastMovementDate = latest?.MovementDate
        };

        private static CashRegisterOperationDto OpenResult(CashMovement movement) => new()
        {
            MovementId = movement.Id,
            Message = "Caja abierta exitosamente",
            CurrentBalance = movement.BalanceAfter
        };

        private static CashRegisterOperationDto CloseResult(CashMovement movement) => new()
        {
            MovementId = movement.Id,
            Message = "Caja cerrada exitosamente",
            CurrentBalance = 0m,
            ExpectedAmount = movement.ExpectedAmount,
            CountedAmount = movement.CountedAmount,
            Difference = movement.Difference
        };
    }
}
