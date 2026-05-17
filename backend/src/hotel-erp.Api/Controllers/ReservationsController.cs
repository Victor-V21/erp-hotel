using AutoMapper;
using hotel_erp.Application.DTOs;
using hotel_erp.Application.Interfaces;
using hotel_erp.Application.Services;
using hotel_erp.Domain.Entities;
using hotel_erp.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace hotel_erp.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ReservationsController : ControllerBase
    {
        private readonly IReservationRepository _repo;
        private readonly IRoomRepository _roomRepo;
        private readonly IFolioRepository _folioRepo;
        private readonly IGuestRepository _guestRepo;
        private readonly IInvoiceRepository _invoiceRepo;
        private readonly ICAIRepository _caiRepo;
        private readonly IBusinessSettingsRepository _settingsRepo;
        private readonly IDiscountRepository _discountRepo;
        private readonly TaxService _taxService;
        private readonly IMapper _mapper;

        public ReservationsController(
            IReservationRepository repo,
            IRoomRepository roomRepo,
            IFolioRepository folioRepo,
            IGuestRepository guestRepo,
            IInvoiceRepository invoiceRepo,
            ICAIRepository caiRepo,
            IBusinessSettingsRepository settingsRepo,
            IDiscountRepository discountRepo,
            TaxService taxService,
            IMapper mapper)
        {
            _repo = repo;
            _roomRepo = roomRepo;
            _folioRepo = folioRepo;
            _guestRepo = guestRepo;
            _invoiceRepo = invoiceRepo;
            _caiRepo = caiRepo;
            _settingsRepo = settingsRepo;
            _discountRepo = discountRepo;
            _taxService = taxService;
            _mapper = mapper;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<ReservationDto>>> GetAll([FromQuery] string? status)
        {
            if (!string.IsNullOrEmpty(status))
                return Ok(_mapper.Map<IEnumerable<ReservationDto>>(await _repo.GetByStatusAsync(status)));
            return Ok(_mapper.Map<IEnumerable<ReservationDto>>(await _repo.GetAllAsync()));
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<ReservationDto>> GetById(Guid id)
        {
            var reservation = await _repo.GetByIdAsync(id);
            if (reservation == null) return NotFound();
            return Ok(_mapper.Map<ReservationDto>(reservation));
        }

        [HttpGet("date-range")]
        public async Task<ActionResult<IEnumerable<ReservationDto>>> GetByDateRange([FromQuery] DateOnly start, [FromQuery] DateOnly end)
            => Ok(_mapper.Map<IEnumerable<ReservationDto>>(await _repo.GetByDateRangeAsync(start, end)));

        [HttpGet("calendar")]
        public async Task<ActionResult<IEnumerable<ReservationDto>>> GetCalendar([FromQuery] DateOnly? start, [FromQuery] DateOnly? end)
        {
            var from = start ?? DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(-1));
            var to = end ?? DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(1));
            return Ok(_mapper.Map<IEnumerable<ReservationDto>>(await _repo.GetByDateRangeAsync(from, to)));
        }

        [HttpPost]
        public async Task<ActionResult<ReservationDto>> Create([FromBody] CreateReservationRequest request)
        {
            // Validate room availability
            var availableRooms = await _roomRepo.GetAvailableAsync(request.CheckInDate, request.CheckOutDate);
            if (!availableRooms.Any(r => r.Id == request.RoomId))
                return BadRequest("La habitación no está disponible para las fechas seleccionadas");

            var reservation = new Reservation
            {
                GuestId = request.GuestId,
                RoomId = request.RoomId,
                CheckInDate = request.CheckInDate,
                CheckOutDate = request.CheckOutDate,
                Adults = request.Adults,
                Children = request.Children,
                PaymentMethod = request.PaymentMethod,
                AdvancePayment = request.AdvancePayment,
                Notes = request.Notes,
                Status = ReservationStatus.Pendiente
            };

            await _repo.AddAsync(reservation);

            // Mark room as reserved
            var room = await _roomRepo.GetByIdAsync(request.RoomId);
            if (room != null)
            {
                room.Status = RoomStatus.Reservada;
                await _roomRepo.UpdateAsync(room);
            }

            return CreatedAtAction(nameof(GetById), new { id = reservation.Id }, _mapper.Map<ReservationDto>(reservation));
        }

        [HttpPut("{id}")]
        public async Task<ActionResult> Update(Guid id, [FromBody] UpdateReservationRequest request)
        {
            var reservation = await _repo.GetByIdAsync(id);
            if (reservation == null) return NotFound();
            if (reservation.Status == ReservationStatus.CheckIn || reservation.Status == ReservationStatus.CheckOut)
                return BadRequest("No se puede modificar una reserva en curso");

            if (request.RoomId.HasValue) reservation.RoomId = request.RoomId.Value;
            if (request.CheckInDate.HasValue) reservation.CheckInDate = request.CheckInDate.Value;
            if (request.CheckOutDate.HasValue) reservation.CheckOutDate = request.CheckOutDate.Value;
            if (request.Adults.HasValue) reservation.Adults = request.Adults.Value;
            if (request.Children.HasValue) reservation.Children = request.Children.Value;
            if (request.PaymentMethod != null) reservation.PaymentMethod = request.PaymentMethod;
            if (request.AdvancePayment.HasValue) reservation.AdvancePayment = request.AdvancePayment.Value;
            if (request.Notes != null) reservation.Notes = request.Notes;
            await _repo.UpdateAsync(reservation);
            return NoContent();
        }

        [HttpPost("{id}/confirm")]
        public async Task<ActionResult> Confirm(Guid id)
        {
            var reservation = await _repo.GetByIdAsync(id);
            if (reservation == null) return NotFound();
            reservation.Status = ReservationStatus.Confirmada;
            await _repo.UpdateAsync(reservation);
            return Ok(new { message = "Reserva confirmada" });
        }

        [HttpPost("{id}/cancel")]
        public async Task<ActionResult> Cancel(Guid id)
        {
            var reservation = await _repo.GetByIdAsync(id);
            if (reservation == null) return NotFound();

            reservation.Status = ReservationStatus.Cancelada;

            // Free the room
            var room = await _roomRepo.GetByIdAsync(reservation.RoomId);
            if (room != null)
            {
                room.Status = RoomStatus.Libre;
                await _roomRepo.UpdateAsync(room);
            }

            await _repo.UpdateAsync(reservation);
            return Ok(new { message = "Reserva cancelada" });
        }

        [HttpPost("checkin")]
        public async Task<ActionResult> CheckIn([FromBody] CheckInRequest request)
        {
            var reservation = await _repo.GetByIdAsync(request.ReservationId);
            if (reservation == null) return NotFound("Reserva no encontrada");
            if (reservation.Status == ReservationStatus.Cancelada)
                return BadRequest("La reserva está cancelada");

            // Load business settings for tax rates
            var settings = await _settingsRepo.GetAsync();
            if (settings != null)
            {
                _taxService.IsvRate = settings.IsvRate;
                _taxService.TouristTaxRate = settings.TouristTaxRate;
            }

            reservation.Status = ReservationStatus.CheckIn;
            reservation.RoomId = request.RoomId;

            var room = await _roomRepo.GetByIdAsync(request.RoomId);
            if (room == null) return NotFound("Habitación no encontrada");
            room.Status = RoomStatus.Ocupada;
            await _roomRepo.UpdateAsync(room);
            await _repo.UpdateAsync(reservation);

            var nights = (reservation.CheckOutDate.DayNumber - reservation.CheckInDate.DayNumber);
            if (nights <= 0) return BadRequest("La estadía debe ser al menos 1 noche");

            var sellingPricePerNight = room.RoomType?.PricePerNight ?? 0;
            var taxResult = _taxService.CalculateFromSellingPrice(sellingPricePerNight, nights);

            // Apply discounts
            decimal totalDiscountPercent = 0;
            if (request.DiscountIds?.Any() == true)
            {
                var discounts = await _discountRepo.GetActiveAsync();
                var selectedDiscounts = discounts.Where(d => request.DiscountIds.Contains(d.Id)).OrderBy(d => d.Priority).ToList();
                foreach (var d in selectedDiscounts)
                {
                    if (d.DiscountType == Domain.Enums.DiscountType.Porcentaje)
                        totalDiscountPercent += d.Value;
                }
                taxResult = _taxService.ApplyDiscount(taxResult, totalDiscountPercent);
            }

            // Create folio
            var folio = new Folio
            {
                ReservationId = reservation.Id,
                GuestId = reservation.GuestId,
                RoomId = request.RoomId,
                OpeningDate = HondurasTime.Now,
                Status = FolioStatus.Abierto
            };

            folio.FolioItems.Add(new FolioItem
            {
                Description = $"Hospedaje - {room.RoomNumber} x {nights} noche{(nights > 1 ? "s" : "")}",
                Quantity = 1,
                UnitPrice = taxResult.Subtotal,
                LineTotal = taxResult.Subtotal,
                IsExempt = false,
                ISVRate = _taxService.IsvRate,
                IsTouristTaxable = true
            });

            await _folioRepo.AddAsync(folio);

            // Validate CAI
            var cai = await _caiRepo.GetActiveCAIAsync();
            if (cai == null) return BadRequest("No hay un CAI activo");
            if (cai.DueDate <= HondurasTime.Today)
                return BadRequest("El CAI está vencido");
            if (int.Parse(cai.CurrentCorrelative.Split('-').Last()) >= int.Parse(cai.FinalRange.Split('-').Last()))
                return BadRequest("El CAI ha agotado su rango");

            var correlative = await _invoiceRepo.GetNextCorrelativeAsync(cai.Id);
            var guest = await _guestRepo.GetByIdAsync(reservation.GuestId);

            // Create invoice with SAR breakdown
            var invoice = new Invoice
            {
                CAIId = cai.Id,
                CorrelativeNumber = correlative,
                CustomerId = null,
                GuestId = reservation.GuestId,
                RTNCliente = guest?.RTN ?? "C/F",
                CustomerName = guest != null ? $"{guest.FirstName} {guest.LastName}" : "",
                CustomerAddress = guest?.Origin ?? "",
                SubTotal = taxResult.Subtotal,
                ISVAmount = taxResult.ISV,
                TouristTaxAmount = taxResult.TouristTax,
                DiscountsAmount = taxResult.DiscountAmount,
                TotalAmount = taxResult.Total,
                InvoiceDate = HondurasTime.Now,
                DocumentType = InvoiceDocumentType.Factura,
                Status = InvoiceStatus.Pagada,
                PaymentMethod = request.PaymentMethod ?? "Efectivo",
                CashReceived = request.CashReceived,
                CashChange = request.CashChange,
                InvoiceItems = new List<InvoiceItem>
                {
                    new()
                    {
                        Description = $"{(room.RoomType?.Name ?? $"Hab. {room.RoomNumber}")}|Hospedaje x{nights} noche(s){(totalDiscountPercent > 0 ? $" (Desc. {totalDiscountPercent}%)" : "")}",
                        Quantity = nights,
                        UnitPrice = sellingPricePerNight,
                        LineTotal = taxResult.Total,
                        IsExempt = false,
                        ISVRate = _taxService.IsvRate,
                        IsTouristTaxable = true,
                        DiscountPercentage = totalDiscountPercent
                    }
                }
            };

            await _invoiceRepo.AddAsync(invoice);

            folio.TotalAmount = taxResult.Total;
            await _folioRepo.UpdateAsync(folio);

            return Ok(new
            {
                message = "Check-in exitoso",
                folioId = folio.Id,
                invoiceId = invoice.Id,
                correlative = invoice.CorrelativeNumber,
                sellingPricePerNight,
                nights,
                subtotal = taxResult.Subtotal,
                isv = taxResult.ISV,
                isvRate = _taxService.IsvRate,
                touristTax = taxResult.TouristTax,
                touristTaxRate = _taxService.TouristTaxRate,
                discountPercent = totalDiscountPercent,
                discountAmount = taxResult.DiscountAmount,
                total = taxResult.Total
            });
        }

        [HttpPost("checkout/{id}")]
        public async Task<ActionResult> CheckOut(Guid id)
        {
            var reservation = await _repo.GetByIdAsync(id);
            if (reservation == null) return NotFound("Reserva no encontrada");
            if (reservation.Status != ReservationStatus.CheckIn)
                return BadRequest("La reserva no está en estado Check-In");

            var folio = await _folioRepo.GetByReservationAsync(id);
            if (folio != null)
            {
                folio.ClosingDate = DateTime.UtcNow;
                folio.Status = FolioStatus.Cerrado;
                await _folioRepo.UpdateAsync(folio);
            }

            // Free room
            var room = await _roomRepo.GetByIdAsync(reservation.RoomId);
            if (room != null)
            {
                room.Status = RoomStatus.Limpieza;
                await _roomRepo.UpdateAsync(room);
            }

            reservation.Status = ReservationStatus.CheckOut;
            await _repo.UpdateAsync(reservation);

            return Ok(new
            {
                message = "Check-out exitoso, habitación liberada"
            });
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult> Delete(Guid id)
        {
            await _repo.DeleteAsync(id);
            return NoContent();
        }
    }
}
