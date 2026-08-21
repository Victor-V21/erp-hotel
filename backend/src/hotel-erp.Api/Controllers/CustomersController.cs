using hotel_erp.Api.Dtos.Customer;
using AutoMapper;
using hotel_erp.Api.Dtos.Common;
using hotel_erp.Api.Services.Interfaces;
using hotel_erp.Api.Database.Entities;
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
        private readonly IInvoiceRepository _invoiceRepo;
        private readonly IMapper _mapper;

        public CustomersController(ICustomerRepository repo, IInvoiceRepository invoiceRepo, IMapper mapper)
        {
            _repo = repo;
            _invoiceRepo = invoiceRepo;
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

            var taxpayerType = Enum.TryParse<TaxpayerType>(request.TaxpayerType, out var parsedType) ? parsedType : TaxpayerType.Gravado;
            if (taxpayerType == TaxpayerType.Exonerado && (string.IsNullOrWhiteSpace(request.ExonerationOrderNumber) || string.IsNullOrWhiteSpace(request.SefinExonerationCertificateNumber)))
                return BadRequest("Cliente exonerado requiere O.C. Exenta y Constancia SEFIN");

            var entity = new hotel_erp.Api.Database.Entities.Customer
            {
                RTN = request.RTN,
                Name = request.Name,
                Address = request.Address,
                Phone = request.Phone,
                Email = request.Email,
                TaxpayerType = taxpayerType,
                ExonerationOrderNumber = request.ExonerationOrderNumber,
                SefinExonerationCertificateNumber = request.SefinExonerationCertificateNumber,
                SagRegistryNumber = request.SagRegistryNumber,
                IsIsvExempt = request.IsIsvExempt,
                IsTouristTaxExempt = request.IsTouristTaxExempt,
                ExonerationValidFrom = request.ExonerationValidFrom,
                ExonerationValidTo = request.ExonerationValidTo
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
            if (request.TaxpayerType != null && Enum.TryParse<TaxpayerType>(request.TaxpayerType, out var taxpayerType)) entity.TaxpayerType = taxpayerType;
            if (request.ExonerationOrderNumber != null) entity.ExonerationOrderNumber = request.ExonerationOrderNumber;
            if (request.SefinExonerationCertificateNumber != null) entity.SefinExonerationCertificateNumber = request.SefinExonerationCertificateNumber;
            if (request.SagRegistryNumber != null) entity.SagRegistryNumber = request.SagRegistryNumber;
            if (request.IsIsvExempt.HasValue) entity.IsIsvExempt = request.IsIsvExempt.Value;
            if (request.IsTouristTaxExempt.HasValue) entity.IsTouristTaxExempt = request.IsTouristTaxExempt.Value;
            if (request.ExonerationValidFrom.HasValue) entity.ExonerationValidFrom = request.ExonerationValidFrom;
            if (request.ExonerationValidTo.HasValue) entity.ExonerationValidTo = request.ExonerationValidTo;
            if (entity.TaxpayerType == TaxpayerType.Exonerado && (string.IsNullOrWhiteSpace(entity.ExonerationOrderNumber) || string.IsNullOrWhiteSpace(entity.SefinExonerationCertificateNumber)))
                return BadRequest("Cliente exonerado requiere O.C. Exenta y Constancia SEFIN");
            await _repo.UpdateAsync(entity);
            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult> Delete(Guid id)
        {
            var invoices = await _invoiceRepo.GetByCustomerAsync(id);
            if (invoices.Any())
                return BadRequest("No se puede eliminar el cliente porque tiene facturas emitidas (trazabilidad fiscal).");

            await _repo.DeleteAsync(id);
            return NoContent();
        }
    }
}



