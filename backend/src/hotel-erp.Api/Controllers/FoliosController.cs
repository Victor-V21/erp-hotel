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
    public class FoliosController : ControllerBase
    {
        private readonly IFolioRepository _repo;
        private readonly IMapper _mapper;

        public FoliosController(IFolioRepository repo, IMapper mapper)
        {
            _repo = repo;
            _mapper = mapper;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<FolioDto>>> GetAll()
            => Ok(_mapper.Map<IEnumerable<FolioDto>>(await _repo.GetAllAsync()));

        [HttpGet("{id}")]
        public async Task<ActionResult<FolioDto>> GetById(Guid id)
        {
            var folio = await _repo.GetByIdAsync(id);
            if (folio == null) return NotFound();
            return Ok(_mapper.Map<FolioDto>(folio));
        }

        [HttpGet("by-reservation/{reservationId}")]
        public async Task<ActionResult<FolioDto>> GetByReservation(Guid reservationId)
        {
            var folio = await _repo.GetByReservationAsync(reservationId);
            if (folio == null) return NotFound();
            return Ok(_mapper.Map<FolioDto>(folio));
        }

        [HttpPost("{folioId}/items")]
        public async Task<ActionResult> AddItem(Guid folioId, [FromBody] AddFolioItemRequest request)
        {
            var folio = await _repo.GetByIdAsync(folioId);
            if (folio == null) return NotFound();
            if (folio.Status == hotel_erp.Api.Database.Entities.FolioStatus.Cerrado)
                return BadRequest("El folio está cerrado");

            var item = new FolioItem
            {
                FolioId = folioId,
                Description = request.Description,
                Quantity = request.Quantity,
                UnitPrice = request.UnitPrice,
                LineTotal = request.Quantity * request.UnitPrice,
                IsExempt = request.IsExempt,
                ISVRate = request.ISVRate,
                IsTouristTaxable = request.IsTouristTaxable,
                DiscountPercentage = request.DiscountPercentage
            };

            folio.FolioItems.Add(item);
            folio.TotalAmount = folio.FolioItems.Sum(fi =>
            {
                var line = fi.LineTotal - (fi.LineTotal * fi.DiscountPercentage / 100m);
                var tax = fi.IsExempt ? 0 : line * fi.ISVRate;
                var tourist = fi.IsTouristTaxable ? line * 0.04m : 0;
                return line + tax + tourist;
            });

            await _repo.UpdateAsync(folio);
            return Ok(new { message = "Item agregado al folio" });
        }
    }
}


