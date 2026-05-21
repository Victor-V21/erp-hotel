using AutoMapper;
using hotel_erp.Api.Dtos.Cash;
using hotel_erp.Api.Services.Interfaces;
using hotel_erp.Api.Database.Entities;
using hotel_erp.Api.Database.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace hotel_erp.Api.Controllers
{
    [ApiController]
    [Route("api/cash-registers")]
    [Authorize]
    public class CashRegistersController : ControllerBase
    {
        private readonly ICashRegisterRepository _repo;
        private readonly ICashMovementRepository _movementRepo;
        private readonly IMapper _mapper;

        public CashRegistersController(
            ICashRegisterRepository repo,
            ICashMovementRepository movementRepo,
            IMapper mapper)
        {
            _repo = repo;
            _movementRepo = movementRepo;
            _mapper = mapper;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<CashRegisterDto>>> GetAll()
            => Ok(_mapper.Map<IEnumerable<CashRegisterDto>>(await _repo.GetAllAsync()));

        [HttpPost]
        public async Task<ActionResult<CashRegisterDto>> Create([FromBody] CreateCashRegisterRequest request)
        {
            var entity = new CashRegister { Name = request.Name, Description = request.Description };
            await _repo.AddAsync(entity);
            return CreatedAtAction(nameof(GetAll), new { id = entity.Id }, _mapper.Map<CashRegisterDto>(entity));
        }

        [HttpPost("{id}/open")]
        public async Task<ActionResult> Open(Guid id, [FromBody] OpenCashRegisterRequest request)
        {
            var register = await _repo.GetByIdAsync(id);
            if (register == null) return NotFound();

            var movement = new CashMovement
            {
                CashRegisterId = id,
                UserId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!),
                MovementType = CashMovementType.Apertura,
                Amount = request.InitialAmount,
                Description = "Apertura de caja",
                BalanceAfter = request.InitialAmount
            };
            await _movementRepo.AddAsync(movement);
            return Ok(new { message = "Caja abierta exitosamente" });
        }

        [HttpPost("{id}/close")]
        public async Task<ActionResult> Close(Guid id, [FromBody] CloseCashRegisterRequest request)
        {
            var register = await _repo.GetByIdAsync(id);
            if (register == null) return NotFound();

            var difference = request.CountedAmount - request.ExpectedAmount;

            var movement = new CashMovement
            {
                CashRegisterId = id,
                UserId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!),
                MovementType = CashMovementType.Cierre,
                Amount = request.CountedAmount,
                Description = $"Cierre de caja. Diferencia: {difference:C}",
                BalanceAfter = 0
            };
            await _movementRepo.AddAsync(movement);

            return Ok(new
            {
                message = "Caja cerrada exitosamente",
                expectedAmount = request.ExpectedAmount,
                countedAmount = request.CountedAmount,
                difference
            });
        }

        [HttpGet("{id}/movements")]
        public async Task<ActionResult<IEnumerable<CashMovementDto>>> GetMovements(Guid id)
            => Ok(_mapper.Map<IEnumerable<CashMovementDto>>(await _movementRepo.GetByRegisterAsync(id)));
    }
}

