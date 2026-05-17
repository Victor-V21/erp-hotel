using AutoMapper;
using hotel_erp.Application.DTOs;
using hotel_erp.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace hotel_erp.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class CustomersController : ControllerBase
    {
        private readonly ICustomerRepository _repo;
        private readonly IMapper _mapper;

        public CustomersController(ICustomerRepository repo, IMapper mapper)
        {
            _repo = repo;
            _mapper = mapper;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<CustomerDto>>> GetAll([FromQuery] string? search)
        {
            if (!string.IsNullOrEmpty(search))
                return Ok(_mapper.Map<IEnumerable<CustomerDto>>(await _repo.SearchAsync(search)));
            return Ok(_mapper.Map<IEnumerable<CustomerDto>>(await _repo.GetAllAsync()));
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<CustomerDto>> GetById(Guid id)
        {
            var customer = await _repo.GetByIdAsync(id);
            if (customer == null) return NotFound();
            return Ok(_mapper.Map<CustomerDto>(customer));
        }

        [HttpPost]
        public async Task<ActionResult<CustomerDto>> Create([FromBody] CreateCustomerRequest request)
        {
            if (!string.IsNullOrEmpty(request.RTN))
            {
                var existing = await _repo.GetByRTNAsync(request.RTN);
                if (existing != null) return BadRequest("El RTN ya existe");
            }

            var entity = new Domain.Entities.Customer
            {
                RTN = request.RTN,
                Name = request.Name,
                Address = request.Address,
                Phone = request.Phone,
                Email = request.Email
            };
            await _repo.AddAsync(entity);
            return CreatedAtAction(nameof(GetById), new { id = entity.Id }, _mapper.Map<CustomerDto>(entity));
        }

        [HttpPut("{id}")]
        public async Task<ActionResult> Update(Guid id, [FromBody] UpdateCustomerRequest request)
        {
            var entity = await _repo.GetByIdAsync(id);
            if (entity == null) return NotFound();

            if (request.RTN != null) entity.RTN = request.RTN;
            if (request.Name != null) entity.Name = request.Name;
            if (request.Address != null) entity.Address = request.Address;
            if (request.Phone != null) entity.Phone = request.Phone;
            if (request.Email != null) entity.Email = request.Email;
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
