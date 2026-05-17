using AutoMapper;
using hotel_erp.Application.DTOs;
using hotel_erp.Application.Interfaces;
using hotel_erp.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace hotel_erp.Api.Controllers
{
    [ApiController]
    [Route("api/tax-configurations")]
    [Authorize]
    public class TaxConfigurationsController : ControllerBase
    {
        private readonly IMapper _mapper;

        // Using Invoice repository for now since TaxConfig doesn't have its own repo
        // In production, create a dedicated repository
        public TaxConfigurationsController(IMapper mapper)
        {
            _mapper = mapper;
        }

        [HttpGet]
        public ActionResult<IEnumerable<TaxConfigurationDto>> GetDefaults()
        {
            var taxes = new List<TaxConfigurationDto>
            {
                new() { Id = Guid.NewGuid(), Name = "ISV 15%", Rate = 15, IsActive = true, ApplicableTo = "General" },
                new() { Id = Guid.NewGuid(), Name = "Impuesto Turístico 4%", Rate = 4, IsActive = true, ApplicableTo = "Hospedaje" },
                new() { Id = Guid.NewGuid(), Name = "Descuento Tercera Edad 25%", Rate = 25, IsActive = true, ApplicableTo = "Hospedaje" }
            };
            return Ok(taxes);
        }
    }
}
