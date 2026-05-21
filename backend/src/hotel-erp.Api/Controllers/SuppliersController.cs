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
    public class SuppliersController : ControllerBase
    {
        private readonly ISupplierRepository _repo;
        private readonly IMapper _mapper;

        public SuppliersController(ISupplierRepository repo, IMapper mapper)
        {
            _repo = repo;
            _mapper = mapper;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<SupplierDto>>> GetAll([FromQuery] string? search)
        {
            if (!string.IsNullOrEmpty(search))
                return Ok(_mapper.Map<IEnumerable<SupplierDto>>(await _repo.SearchAsync(search)));
            return Ok(_mapper.Map<IEnumerable<SupplierDto>>(await _repo.GetAllAsync()));
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<SupplierDto>> GetById(Guid id)
        {
            var supplier = await _repo.GetByIdAsync(id);
            if (supplier == null) return NotFound();
            return Ok(_mapper.Map<SupplierDto>(supplier));
        }

        [HttpPost]
        public async Task<ActionResult<SupplierDto>> Create([FromBody] CreateSupplierRequest request)
        {
            var supplier = new hotel_erp.Api.Database.Entities.Supplier
            {
                Name = request.Name,
                RTN = request.RTN,
                ContactPerson = request.ContactPerson,
                Phone = request.Phone,
                Email = request.Email,
                Address = request.Address
            };
            await _repo.AddAsync(supplier);
            return CreatedAtAction(nameof(GetById), new { id = supplier.Id }, _mapper.Map<SupplierDto>(supplier));
        }

        [HttpPut("{id}")]
        public async Task<ActionResult> Update(Guid id, [FromBody] UpdateSupplierRequest request)
        {
            var supplier = await _repo.GetByIdAsync(id);
            if (supplier == null) return NotFound();

            if (request.Name != null) supplier.Name = request.Name;
            if (request.RTN != null) supplier.RTN = request.RTN;
            if (request.ContactPerson != null) supplier.ContactPerson = request.ContactPerson;
            if (request.Phone != null) supplier.Phone = request.Phone;
            if (request.Email != null) supplier.Email = request.Email;
            if (request.Address != null) supplier.Address = request.Address;
            if (request.IsActive.HasValue) supplier.IsActive = request.IsActive.Value;

            await _repo.UpdateAsync(supplier);
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


