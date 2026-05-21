using AutoMapper;
using hotel_erp.Api.Dtos.Common;
using hotel_erp.Api.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace hotel_erp.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class RoomsController : ControllerBase
    {
        private readonly IRoomRepository _repo;
        private readonly IMapper _mapper;

        public RoomsController(IRoomRepository repo, IMapper mapper)
        {
            _repo = repo;
            _mapper = mapper;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<RoomDto>>> GetAll()
            => Ok(_mapper.Map<IEnumerable<RoomDto>>(await _repo.GetAllAsync()));

        [HttpGet("{id}")]
        public async Task<ActionResult<RoomDto>> GetById(Guid id)
        {
            var room = await _repo.GetByIdAsync(id);
            if (room == null) return NotFound();
            return Ok(_mapper.Map<RoomDto>(room));
        }

        [HttpGet("available")]
        public async Task<ActionResult<IEnumerable<RoomDto>>> GetAvailable([FromQuery] DateOnly checkIn, [FromQuery] DateOnly checkOut)
            => Ok(_mapper.Map<IEnumerable<RoomDto>>(await _repo.GetAvailableAsync(checkIn, checkOut)));

        [HttpGet("status/{status}")]
        public async Task<ActionResult<IEnumerable<RoomDto>>> GetByStatus(string status)
            => Ok(_mapper.Map<IEnumerable<RoomDto>>(await _repo.GetByStatusAsync(status)));

        [HttpPost]
        public async Task<ActionResult<RoomDto>> Create([FromBody] CreateRoomRequest request)
        {
            var existing = await _repo.GetByRoomNumberAsync(request.RoomNumber);
            if (existing != null) return BadRequest("El número de habitación ya existe");

            var room = new hotel_erp.Api.Database.Entities.Room
            {
                RoomNumber = request.RoomNumber,
                Floor = request.Floor,
                RoomTypeId = request.RoomTypeId,
                Observations = request.Observations
            };
            await _repo.AddAsync(room);
            return CreatedAtAction(nameof(GetById), new { id = room.Id }, _mapper.Map<RoomDto>(room));
        }

        [HttpPut("{id}")]
        public async Task<ActionResult> Update(Guid id, [FromBody] UpdateRoomRequest request)
        {
            var room = await _repo.GetByIdAsync(id);
            if (room == null) return NotFound();

            if (request.RoomNumber != null) room.RoomNumber = request.RoomNumber;
            if (request.Floor.HasValue) room.Floor = request.Floor.Value;
            if (request.RoomTypeId.HasValue) room.RoomTypeId = request.RoomTypeId.Value;
            if (request.Status != null && Enum.TryParse<hotel_erp.Api.Database.Entities.RoomStatus>(request.Status, out var parsedStatus))
                room.Status = parsedStatus;
            if (request.Observations != null) room.Observations = request.Observations;
            await _repo.UpdateAsync(room);
            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult> Delete(Guid id)
        {
            await _repo.DeleteAsync(id);
            return NoContent();
        }
    }
}


