using AutoMapper;
using hotel_erp.Api.Authorization;
using hotel_erp.Api.Dtos.Common;
using hotel_erp.Api.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace hotel_erp.Api.Controllers
{
    [ApiController]
    [Route("api/room-types")]
    [Authorize]
    public class RoomTypesController : ControllerBase
    {
        private readonly IRoomTypeRepository _repo;
        private readonly IMapper _mapper;

        public RoomTypesController(IRoomTypeRepository repo, IMapper mapper)
        {
            _repo = repo;
            _mapper = mapper;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<RoomTypeDto>>> GetAll()
            => Ok(_mapper.Map<IEnumerable<RoomTypeDto>>(await _repo.GetAllAsync()));

        [HttpGet("{id}")]
        public async Task<ActionResult<RoomTypeDto>> GetById(Guid id)
        {
            var entity = await _repo.GetByIdAsync(id);
            if (entity == null) return NotFound();
            return Ok(_mapper.Map<RoomTypeDto>(entity));
        }

        [HttpPost]
        [Authorize(Policy = PermissionNames.ManageReservations)]
        public async Task<ActionResult<RoomTypeDto>> Create([FromBody] CreateRoomTypeRequest request)
        {
            var existing = await _repo.GetByNameAsync(request.Name);
            if (existing != null) return BadRequest("El tipo de habitación ya existe");

            var entity = new hotel_erp.Api.Database.Entities.RoomType
            {
                Name = request.Name,
                Description = request.Description,
                PricePerNight = request.PricePerNight,
                Capacity = request.Capacity
            };
            await _repo.AddAsync(entity);
            return CreatedAtAction(nameof(GetById), new { id = entity.Id }, _mapper.Map<RoomTypeDto>(entity));
        }

        [HttpPut("{id}")]
        [Authorize(Policy = PermissionNames.ManageReservations)]
        public async Task<ActionResult> Update(Guid id, [FromBody] UpdateRoomTypeRequest request)
        {
            var entity = await _repo.GetByIdAsync(id);
            if (entity == null) return NotFound();

            if (request.Name != null) entity.Name = request.Name;
            if (request.Description != null) entity.Description = request.Description;
            if (request.PricePerNight.HasValue) entity.PricePerNight = request.PricePerNight.Value;
            if (request.Capacity.HasValue) entity.Capacity = request.Capacity.Value;
            await _repo.UpdateAsync(entity);
            return NoContent();
        }

        [HttpDelete("{id}")]
        [Authorize(Policy = PermissionNames.ManageReservations)]
        public async Task<ActionResult> Delete(Guid id)
        {
            await _repo.DeleteAsync(id);
            return NoContent();
        }
    }
}

