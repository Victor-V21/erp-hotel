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
    public class InvoicesController : ControllerBase
    {
        private readonly IInvoiceRepository _repo;
        private readonly ICAIRepository _caiRepo;
        private readonly ICustomerRepository _customerRepo;
        private readonly IMapper _mapper;

        public InvoicesController(
            IInvoiceRepository repo,
            ICAIRepository caiRepo,
            ICustomerRepository customerRepo,
            IMapper mapper)
        {
            _repo = repo;
            _caiRepo = caiRepo;
            _customerRepo = customerRepo;
            _mapper = mapper;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<InvoiceDto>>> GetAll()
            => Ok(_mapper.Map<IEnumerable<InvoiceDto>>(await _repo.GetAllAsync()));

        [HttpGet("{id}")]
        public async Task<ActionResult<InvoiceDto>> GetById(Guid id)
        {
            var invoice = await _repo.GetByIdAsync(id);
            if (invoice == null) return NotFound();
            return Ok(_mapper.Map<InvoiceDto>(invoice));
        }

        [HttpGet("search")]
        public async Task<ActionResult<IEnumerable<InvoiceDto>>> Search([FromQuery] string? dni, [FromQuery] DateTime? start, [FromQuery] DateTime? end)
        {
            if (!string.IsNullOrEmpty(dni))
                return Ok(_mapper.Map<IEnumerable<InvoiceDto>>(await _repo.GetByGuestDocumentAsync(dni)));
            if (start.HasValue && end.HasValue)
                return Ok(_mapper.Map<IEnumerable<InvoiceDto>>(await _repo.GetByDateRangeAsync(start.Value, end.Value)));
            return Ok(_mapper.Map<IEnumerable<InvoiceDto>>(await _repo.GetAllAsync()));
        }

        [HttpGet("date-range")]
        public async Task<ActionResult<IEnumerable<InvoiceDto>>> GetByDateRange([FromQuery] DateTime start, [FromQuery] DateTime end)
            => Ok(_mapper.Map<IEnumerable<InvoiceDto>>(await _repo.GetByDateRangeAsync(start, end)));

        [HttpPut("{id}")]
        public async Task<ActionResult<InvoiceDto>> Update(Guid id, [FromBody] CreateInvoiceRequest request)
        {
            var invoice = await _repo.GetByIdAsync(id);
            if (invoice == null) return NotFound();
            if (invoice.Status == InvoiceStatus.Anulada)
                return BadRequest("No se puede modificar una factura anulada");

            decimal subtotal = 0, isvAmount = 0, touristTax = 0, discounts = 0;

            var items = request.Items.Select(item =>
            {
                var lineTotal = item.Quantity * item.UnitPrice;
                var discount = lineTotal * (item.DiscountPercentage / 100m);
                var lineAfterDiscount = lineTotal - discount;
                subtotal += lineAfterDiscount;
                if (!item.IsExempt) isvAmount += lineAfterDiscount * item.ISVRate;
                if (item.IsTouristTaxable) touristTax += lineAfterDiscount * 0.04m;
                discounts += discount;

                return new InvoiceItem
                {
                    InvoiceId = id,
                    Description = item.Description,
                    Quantity = item.Quantity,
                    UnitPrice = item.UnitPrice,
                    LineTotal = lineTotal,
                    IsExempt = item.IsExempt,
                    ISVRate = item.ISVRate,
                    IsTouristTaxable = item.IsTouristTaxable,
                    DiscountPercentage = item.DiscountPercentage
                };
            }).ToList();

            invoice.CustomerName = request.CustomerName;
            invoice.RTNCliente = request.RTNCliente;
            invoice.CustomerAddress = request.CustomerAddress;
            invoice.SubTotal = Math.Round(subtotal, 2);
            invoice.ISVAmount = Math.Round(isvAmount, 2);
            invoice.TouristTaxAmount = Math.Round(touristTax, 2);
            invoice.DiscountsAmount = Math.Round(discounts, 2);
            invoice.TotalAmount = Math.Round(subtotal + isvAmount + touristTax, 2);

            // Remove old items and add new ones
            _repo.DeleteInvoiceItems(id);
            foreach (var item in items)
                invoice.InvoiceItems.Add(item);

            await _repo.UpdateAsync(invoice);
            return Ok(_mapper.Map<InvoiceDto>(invoice));
        }

        [HttpPost]
        public async Task<ActionResult<InvoiceDto>> Create([FromBody] CreateInvoiceRequest request)
        {
            var cai = await _caiRepo.GetByIdAsync(request.CAIId);
            if (cai == null) return BadRequest("CAI no encontrado");
            if (cai.Status != CAIStatus.Activo) return BadRequest("El CAI no está activo");
            if (cai.DueDate < HondurasTime.Today) return BadRequest("El CAI está vencido");

            // Check correlative range
            var currentSeq = int.Parse(cai.CurrentCorrelative.Split('-').Last());
            var finalSeq = int.Parse(cai.FinalRange.Split('-').Last());
            if (currentSeq > finalSeq) return BadRequest("El CAI ha agotado su rango de correlativos");

            // Get next correlative
            var correlative = await _repo.GetNextCorrelativeAsync(request.CAIId);

            // Calculate taxes
            decimal subtotal = 0;
            decimal isvAmount = 0;
            decimal touristTax = 0;
            decimal discounts = 0;

            var items = request.Items.Select(item =>
            {
                var lineTotal = item.Quantity * item.UnitPrice;
                var discount = lineTotal * (item.DiscountPercentage / 100m);
                var lineAfterDiscount = lineTotal - discount;
                subtotal += lineAfterDiscount;
                if (!item.IsExempt) isvAmount += lineAfterDiscount * item.ISVRate;
                if (item.IsTouristTaxable) touristTax += lineAfterDiscount * 0.04m;
                discounts += discount;

                return new InvoiceItem
                {
                    Description = item.Description,
                    Quantity = item.Quantity,
                    UnitPrice = item.UnitPrice,
                    LineTotal = lineTotal,
                    IsExempt = item.IsExempt,
                    ISVRate = item.ISVRate,
                    IsTouristTaxable = item.IsTouristTaxable,
                    DiscountPercentage = item.DiscountPercentage
                };
            }).ToList();

            var invoice = new Invoice
            {
                CAIId = request.CAIId,
                CorrelativeNumber = correlative,
                CustomerId = request.CustomerId,
                GuestId = request.GuestId,
                RTNCliente = request.RTNCliente,
                CustomerName = request.CustomerName,
                CustomerAddress = request.CustomerAddress,
                SubTotal = Math.Round(subtotal, 2),
                ISVAmount = Math.Round(isvAmount, 2),
                TouristTaxAmount = Math.Round(touristTax, 2),
                DiscountsAmount = Math.Round(discounts, 2),
                TotalAmount = Math.Round(subtotal + isvAmount + touristTax, 2),
                DocumentType = Enum.Parse<InvoiceDocumentType>(request.DocumentType),
                Status = InvoiceStatus.Emitida,
                InvoiceItems = items
            };

            await _repo.AddAsync(invoice);
            return CreatedAtAction(nameof(GetById), new { id = invoice.Id }, _mapper.Map<InvoiceDto>(invoice));
        }

        [HttpPost("{id}/cancel")]
        public async Task<ActionResult> Cancel(Guid id)
        {
            var invoice = await _repo.GetByIdAsync(id);
            if (invoice == null) return NotFound();
            invoice.Status = InvoiceStatus.Anulada;
            await _repo.UpdateAsync(invoice);
            return Ok(new { message = "Factura anulada" });
        }
    }
}
