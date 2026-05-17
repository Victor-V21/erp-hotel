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
    public class GuestsController : ControllerBase
    {
        private readonly IGuestRepository _repo;
        private readonly IReservationRepository _reservationRepo;
        private readonly IMapper _mapper;

        public GuestsController(IGuestRepository repo, IReservationRepository reservationRepo, IMapper mapper)
        {
            _repo = repo;
            _reservationRepo = reservationRepo;
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

            var entity = new Domain.Entities.Guest
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
                Classification = request.Classification
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
            await _repo.UpdateAsync(entity);
            return NoContent();
        }

        [HttpGet("{id}/stats")]
        public async Task<ActionResult<GuestStatsDto>> GetStats(Guid id)
        {
            var guest = await _repo.GetByIdAsync(id);
            if (guest == null) return NotFound();

            var reservations = await _reservationRepo.GetByGuestAsync(id);
            var completedVisits = reservations.Where(r => r.Status == Domain.Enums.ReservationStatus.CheckOut).ToList();
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
            await _repo.DeleteAsync(id);
            return NoContent();
        }
    }
}
