using AutoMapper;
using hotel_erp.Api.Dtos.Common;
using hotel_erp.Api.Database;
using hotel_erp.Api.Database.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace hotel_erp.Api.Controllers
{
    [ApiController]
    [Route("api/tax-configurations")]
    [Authorize]
    public class TaxConfigurationsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IMapper _mapper;

        public TaxConfigurationsController(ApplicationDbContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<TaxConfigurationDto>>> GetAll()
        {
            var taxes = await _context.TaxConfigurations.OrderBy(t => t.Name).ToListAsync();
            return Ok(_mapper.Map<IEnumerable<TaxConfigurationDto>>(taxes));
        }

        [HttpPost]
        public async Task<ActionResult<TaxConfigurationDto>> Create([FromBody] CreateTaxConfigurationRequest request)
        {
            var existing = await _context.TaxConfigurations.FirstOrDefaultAsync(t => t.Name == request.Name);
            if (existing != null) return BadRequest("Ya existe una configuración fiscal con ese nombre");

            var tax = new TaxConfiguration
            {
                Name = request.Name,
                Rate = request.Rate,
                IsActive = request.IsActive,
                ApplicableTo = request.ApplicableTo
            };
            await _context.TaxConfigurations.AddAsync(tax);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(GetAll), new { id = tax.Id }, _mapper.Map<TaxConfigurationDto>(tax));
        }

        [HttpPut("{id}")]
        public async Task<ActionResult> Update(Guid id, [FromBody] CreateTaxConfigurationRequest request)
        {
            var tax = await _context.TaxConfigurations.FindAsync(id);
            if (tax == null) return NotFound();

            tax.Name = request.Name;
            tax.Rate = request.Rate;
            tax.IsActive = request.IsActive;
            tax.ApplicableTo = request.ApplicableTo;
            await _context.SaveChangesAsync();
            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult> Delete(Guid id)
        {
            var tax = await _context.TaxConfigurations.FindAsync(id);
            if (tax == null) return NotFound();

            tax.IsDeleted = true;
            await _context.SaveChangesAsync();
            return NoContent();
        }
    }
}

