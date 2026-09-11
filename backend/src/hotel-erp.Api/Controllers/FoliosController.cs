using System.Security.Claims;
using AutoMapper;
using hotel_erp.Api.Authorization;
using hotel_erp.Api.Database;
using hotel_erp.Api.Dtos.Common;
using hotel_erp.Api.Services;
using hotel_erp.Api.Services.Interfaces;
using hotel_erp.Api.Database.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace hotel_erp.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class FoliosController : ControllerBase
    {
        private readonly IFolioRepository _repo;
        private readonly ApplicationDbContext _context;
        private readonly AuditService _auditService;
        private readonly IMapper _mapper;

        public FoliosController(
            IFolioRepository repo,
            ApplicationDbContext context,
            AuditService auditService,
            IMapper mapper)
        {
            _repo = repo;
            _context = context;
            _auditService = auditService;
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
        [Authorize(Policy = PermissionNames.CreateInvoices)]
        public async Task<ActionResult> AddItem(Guid folioId, [FromBody] AddFolioItemRequest request)
        {
            await using var transaction = await _context.Database.BeginTransactionAsync();
            var folioLock = $"folio-settlement:{folioId:N}";
            await _context.Database.ExecuteSqlInterpolatedAsync(
                $"SELECT pg_advisory_xact_lock(hashtext({folioLock}));");

            var folio = await _context.Folios
                .Include(candidate => candidate.FolioItems)
                .SingleOrDefaultAsync(candidate => candidate.Id == folioId);
            if (folio == null) return NotFound();
            if (folio.Status != FolioStatus.Abierto)
                return Conflict("El folio no está abierto para recibir cargos");

            InvoiceCalculation calculation;
            try
            {
                calculation = InvoiceCalculationService.Calculate(
                    folio.FolioItems.Select(item => new InvoiceLineInput(
                            item.Description,
                            item.Quantity,
                            item.UnitPrice,
                            item.IsExempt,
                            item.ISVRate,
                            item.IsTouristTaxable,
                            item.DiscountPercentage))
                        .Append(new InvoiceLineInput(
                            request.Description,
                            request.Quantity,
                            request.UnitPrice,
                            request.IsExempt,
                            request.ISVRate,
                            request.IsTouristTaxable,
                            request.DiscountPercentage)));
            }
            catch (InvoiceCalculationException ex)
            {
                return BadRequest(ex.Message);
            }

            var item = new FolioItem
            {
                FolioId = folioId,
                Description = request.Description.Trim(),
                Quantity = request.Quantity,
                UnitPrice = TaxService.RoundCurrency(request.UnitPrice),
                LineTotal = TaxService.RoundCurrency(request.Quantity * request.UnitPrice),
                IsExempt = request.IsExempt,
                ISVRate = request.ISVRate,
                IsTouristTaxable = request.IsTouristTaxable,
                DiscountPercentage = request.DiscountPercentage
            };
            folio.FolioItems.Add(item);
            folio.TotalAmount = calculation.TotalAmount;

            await _context.SaveChangesAsync();
            var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            await _auditService.LogAsync(
                userId,
                "AddFolioCharge",
                nameof(Folio),
                folio.Id,
                new { ItemId = item.Id, item.Description, item.Quantity, item.UnitPrice, folio.TotalAmount });
            await transaction.CommitAsync();
            return Ok(new { message = "Item agregado al folio", itemId = item.Id, totalAmount = folio.TotalAmount });
        }
    }
}
