using hotel_erp.Api.Dtos.Discount;
using AutoMapper;
using hotel_erp.Api.Dtos.Common;
using hotel_erp.Api.Services.Interfaces;
using hotel_erp.Api.Database.Entities;
using hotel_erp.Api.Database.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace hotel_erp.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class DiscountsController : ControllerBase
    {
        private readonly IDiscountRepository _repo;
        private readonly IMapper _mapper;

        public DiscountsController(IDiscountRepository repo, IMapper mapper)
        {
            _repo = repo;
            _mapper = mapper;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<DiscountDto>>> GetAll()
            => Ok(_mapper.Map<IEnumerable<DiscountDto>>(await _repo.GetAllAsync()));

        [HttpGet("{id}")]
        public async Task<ActionResult<DiscountDto>> GetById(Guid id)
        {
            var discount = await _repo.GetByIdAsync(id);
            if (discount == null) return NotFound();
            return Ok(_mapper.Map<DiscountDto>(discount));
        }

        [HttpPost]
        public async Task<ActionResult<DiscountDto>> Create([FromBody] CreateDiscountRequest request)
        {
            var entity = new Discount
            {
                Name = request.Name,
                Description = request.Description,
                DiscountType = Enum.Parse<DiscountType>(request.DiscountType),
                Value = request.Value,
                IsActive = request.IsActive,
                ApplicableTo = request.ApplicableTo,
                RequiresDocument = request.RequiresDocument,
                MinAge = request.MinAge,
                Priority = request.Priority
            };
            await _repo.AddAsync(entity);
            return CreatedAtAction(nameof(GetById), new { id = entity.Id }, _mapper.Map<DiscountDto>(entity));
        }

        [HttpPut("{id}")]
        public async Task<ActionResult> Update(Guid id, [FromBody] UpdateDiscountRequest request)
        {
            var entity = await _repo.GetByIdAsync(id);
            if (entity == null) return NotFound();

            if (request.Name != null) entity.Name = request.Name;
            if (request.Description != null) entity.Description = request.Description;
            if (request.DiscountType != null) entity.DiscountType = Enum.Parse<DiscountType>(request.DiscountType);
            if (request.Value.HasValue) entity.Value = request.Value.Value;
            if (request.IsActive.HasValue) entity.IsActive = request.IsActive.Value;
            if (request.ApplicableTo != null) entity.ApplicableTo = request.ApplicableTo;
            if (request.RequiresDocument.HasValue) entity.RequiresDocument = request.RequiresDocument.Value;
            if (request.MinAge.HasValue) entity.MinAge = request.MinAge.Value;
            if (request.Priority.HasValue) entity.Priority = request.Priority.Value;

            await _repo.UpdateAsync(entity);
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


