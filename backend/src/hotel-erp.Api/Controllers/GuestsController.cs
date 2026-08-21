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
    public class GuestsController : ControllerBase
    {
        private readonly IGuestRepository _repo;
        private readonly IReservationRepository _reservationRepo;
        private readonly IInvoiceRepository _invoiceRepo;
        private readonly IMapper _mapper;

        public GuestsController(IGuestRepository repo, IReservationRepository reservationRepo, IInvoiceRepository invoiceRepo, IMapper mapper)
        {
            _repo = repo;
            _reservationRepo = reservationRepo;
            _invoiceRepo = invoiceRepo;
            _mapper = mapper;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<GuestDto>>> GetAll([FromQuery] string? search)
        {
            if (!string.IsNullOrEmpty(search))
                return Ok(_mapper.Map<IEnumerable<GuestDto>>(await _repo.SearchAsync(search)));
            return Ok(_mapper.Map<IEnumerable<GuestDto>>(await _repo.GetAllAsync()));
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<GuestDto>> GetById(Guid id)
        {
            var guest = await _repo.GetByIdAsync(id);
            if (guest == null) return NotFound();
            return Ok(_mapper.Map<GuestDto>(guest));
        }

        [HttpPost]
        public async Task<ActionResult<GuestDto>> Create([FromBody] CreateGuestRequest request)
        {
            if (!string.IsNullOrEmpty(request.DocumentNumber))
            {
                var existing = await _repo.GetByDocumentNumberAsync(request.DocumentNumber);
                if (existing != null) return BadRequest("El número de documento ya existe");
            }

            var taxpayerType = Enum.TryParse<TaxpayerType>(request.TaxpayerType, out var parsedType) ? parsedType : TaxpayerType.ConsumidorFinal;
            if (taxpayerType == TaxpayerType.Exonerado && (string.IsNullOrWhiteSpace(request.ExonerationOrderNumber) || string.IsNullOrWhiteSpace(request.SefinExonerationCertificateNumber)))
                return BadRequest("Huésped exonerado requiere O.C. Exenta y Constancia SEFIN");

            var entity = new hotel_erp.Api.Database.Entities.Guest
            {
                FirstName = request.FirstName,
                LastName = request.LastName,
                Email = request.Email,
                Phone = request.Phone,
                DateOfBirth = request.DateOfBirth,
                Nationality = request.Nationality,
                DocumentType = request.DocumentType,
                DocumentNumber = request.DocumentNumber,
                Origin = request.Origin,
                HasVehicle = request.HasVehicle,
                VehiclePlate = request.VehiclePlate,
                Company = request.Company,
                RTN = request.GuestRTN,
                Preferences = request.Preferences,
                Classification = request.Classification,
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
            return CreatedAtAction(nameof(GetById), new { id = entity.Id }, _mapper.Map<GuestDto>(entity));
        }

        [HttpPut("{id}")]
        public async Task<ActionResult> Update(Guid id, [FromBody] UpdateGuestRequest request)
        {
            var entity = await _repo.GetByIdAsync(id);
            if (entity == null) return NotFound();

            if (request.FirstName != null) entity.FirstName = request.FirstName;
            if (request.LastName != null) entity.LastName = request.LastName;
            if (request.Email != null) entity.Email = request.Email;
            if (request.Phone != null) entity.Phone = request.Phone;
            if (request.DateOfBirth.HasValue) entity.DateOfBirth = request.DateOfBirth;
            if (request.Nationality != null) entity.Nationality = request.Nationality;
            if (request.DocumentType != null) entity.DocumentType = request.DocumentType;
            if (request.DocumentNumber != null) entity.DocumentNumber = request.DocumentNumber;
            if (request.Origin != null) entity.Origin = request.Origin;
            if (request.HasVehicle.HasValue) entity.HasVehicle = request.HasVehicle.Value;
            if (request.VehiclePlate != null) entity.VehiclePlate = request.VehiclePlate;
            if (request.Company != null) entity.Company = request.Company;
            if (request.GuestRTN != null) entity.RTN = request.GuestRTN;
            if (request.Preferences != null) entity.Preferences = request.Preferences;
            if (request.Classification != null) entity.Classification = request.Classification;
            if (request.TaxpayerType != null && Enum.TryParse<TaxpayerType>(request.TaxpayerType, out var taxpayerType)) entity.TaxpayerType = taxpayerType;
            if (request.ExonerationOrderNumber != null) entity.ExonerationOrderNumber = request.ExonerationOrderNumber;
            if (request.SefinExonerationCertificateNumber != null) entity.SefinExonerationCertificateNumber = request.SefinExonerationCertificateNumber;
            if (request.SagRegistryNumber != null) entity.SagRegistryNumber = request.SagRegistryNumber;
            if (request.IsIsvExempt.HasValue) entity.IsIsvExempt = request.IsIsvExempt.Value;
            if (request.IsTouristTaxExempt.HasValue) entity.IsTouristTaxExempt = request.IsTouristTaxExempt.Value;
            if (request.ExonerationValidFrom.HasValue) entity.ExonerationValidFrom = request.ExonerationValidFrom;
            if (request.ExonerationValidTo.HasValue) entity.ExonerationValidTo = request.ExonerationValidTo;
            if (entity.TaxpayerType == TaxpayerType.Exonerado && (string.IsNullOrWhiteSpace(entity.ExonerationOrderNumber) || string.IsNullOrWhiteSpace(entity.SefinExonerationCertificateNumber)))
                return BadRequest("Huésped exonerado requiere O.C. Exenta y Constancia SEFIN");
            await _repo.UpdateAsync(entity);
            return NoContent();
        }

        [HttpGet("{id}/stats")]
        public async Task<ActionResult<GuestStatsDto>> GetStats(Guid id)
        {
            var guest = await _repo.GetByIdAsync(id);
            if (guest == null) return NotFound();

            var reservations = await _reservationRepo.GetByGuestAsync(id);
            var completedVisits = reservations.Where(r => r.Status == hotel_erp.Api.Database.Entities.ReservationStatus.CheckOut).ToList();
            var lastVisit = completedVisits.OrderByDescending(r => r.CheckOutDate).FirstOrDefault();
            var totalVisits = completedVisits.Count;
            var isFrequent = totalVisits >= 2;

            return Ok(new GuestStatsDto
            {
                TotalVisits = totalVisits,
                Classification = isFrequent ? "Cliente Frecuente" : guest.Classification ?? "Normal",
                LastVisit = lastVisit?.CheckOutDate.ToString("dd/MM/yyyy"),
                IsFrequent = isFrequent
            });
        }

        [HttpPatch("{id}/classification")]
        public async Task<ActionResult> UpdateClassification(Guid id, [FromBody] UpdateClassificationRequest request)
        {
            var guest = await _repo.GetByIdAsync(id);
            if (guest == null) return NotFound();
            guest.Classification = request.Classification;
            await _repo.UpdateAsync(guest);
            return Ok(new { classification = guest.Classification });
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult> Delete(Guid id)
        {
            var reservations = await _reservationRepo.GetByGuestAsync(id);
            if (reservations.Any())
                return BadRequest("No se puede eliminar el huésped porque tiene reservaciones asociadas.");

            var invoices = await _invoiceRepo.GetByGuestAsync(id);
            if (invoices.Any())
                return BadRequest("No se puede eliminar el huésped porque tiene facturas emitidas (trazabilidad fiscal).");

            await _repo.DeleteAsync(id);
            return NoContent();
        }
    }
}



