using AutoMapper;
using hotel_erp.Api.Authorization;
using hotel_erp.Api.Dtos.Common;
using hotel_erp.Api.Services.Interfaces;
using hotel_erp.Api.Services;
using hotel_erp.Api.Database.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace hotel_erp.Api.Controllers
{
    [ApiController]
    [Route("api/purchase-invoices")]
    [Authorize(Policy = PermissionNames.ManageAccounting)]
    public class PurchaseInvoicesController : ControllerBase
    {
        private readonly IPurchaseInvoiceRepository _repo;
        private readonly ISupplierRepository _supplierRepo;
        private readonly IAccountingService _accountingService;
        private readonly IMapper _mapper;

        public PurchaseInvoicesController(IPurchaseInvoiceRepository repo, ISupplierRepository supplierRepo, IAccountingService accountingService, IMapper mapper)
        {
            _repo = repo;
            _supplierRepo = supplierRepo;
            _accountingService = accountingService;
            _mapper = mapper;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<PurchaseInvoiceDto>>> GetAll([FromQuery] Guid? supplierId)
        {
            if (supplierId.HasValue)
                return Ok(_mapper.Map<IEnumerable<PurchaseInvoiceDto>>(await _repo.GetBySupplierAsync(supplierId.Value)));
            return Ok(_mapper.Map<IEnumerable<PurchaseInvoiceDto>>(await _repo.GetAllAsync()));
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<PurchaseInvoiceDto>> GetById(Guid id)
        {
            var pi = await _repo.GetByIdAsync(id);
            if (pi == null) return NotFound();
            return Ok(_mapper.Map<PurchaseInvoiceDto>(pi));
        }

        [HttpGet("date-range")]
        public async Task<ActionResult<IEnumerable<PurchaseInvoiceDto>>> GetByDateRange([FromQuery] DateTime start, [FromQuery] DateTime end)
            => Ok(_mapper.Map<IEnumerable<PurchaseInvoiceDto>>(await _repo.GetByDateRangeAsync(start, end)));

        [HttpPost]
        public async Task<ActionResult<PurchaseInvoiceDto>> Create([FromBody] CreatePurchaseInvoiceRequest request)
        {
            var supplier = await _supplierRepo.GetByIdAsync(request.SupplierId);
            if (supplier == null) return BadRequest("Proveedor no encontrado");

            decimal subtotal = 0, isv15 = 0;
            var items = request.Items.Select(item =>
            {
                var lineTotal = TaxService.RoundCurrency(item.Quantity * item.UnitPrice);
                subtotal += lineTotal;
                if (!item.IsExempt) isv15 += lineTotal * item.ISVRate;
                return new PurchaseInvoiceItem
                {
                    Description = item.Description,
                    Quantity = item.Quantity,
                    UnitPrice = item.UnitPrice,
                    LineTotal = lineTotal,
                    IsExempt = item.IsExempt,
                    ISVRate = item.ISVRate
                };
            }).ToList();

            var purchaseInvoice = new PurchaseInvoice
            {
                InvoiceNumber = request.InvoiceNumber,
                SupplierId = request.SupplierId,
                InvoiceDate = request.InvoiceDate,
                CAINumber = request.CAINumber,
                SupplierRTN = request.SupplierRTN ?? supplier.RTN,
                SubTotal = TaxService.RoundCurrency(subtotal),
                ISVAmount = TaxService.RoundCurrency(isv15),
                ISV15Amount = TaxService.RoundCurrency(isv15),
                ISV18Amount = 0,
                TotalAmount = TaxService.RoundCurrency(subtotal + isv15),
                Notes = request.Notes,
                Status = PurchaseInvoiceStatus.Pendiente,
                PurchaseInvoiceItems = items
            };

            await _repo.AddAsync(purchaseInvoice);
            await _accountingService.CreatePurchaseEntryAsync(purchaseInvoice);
            return CreatedAtAction(nameof(GetById), new { id = purchaseInvoice.Id }, _mapper.Map<PurchaseInvoiceDto>(purchaseInvoice));
        }

        [HttpPut("{id}/pay")]
        public async Task<ActionResult> MarkAsPaid(Guid id)
        {
            var pi = await _repo.GetByIdAsync(id);
            if (pi == null) return NotFound();
            pi.Status = PurchaseInvoiceStatus.Pagada;
            await _repo.UpdateAsync(pi);
            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult> Delete(Guid id)
        {
            await _accountingService.DeleteEntryByReferenceIdAsync(id);
            await _repo.DeleteAsync(id);
            return NoContent();
        }
    }
}
