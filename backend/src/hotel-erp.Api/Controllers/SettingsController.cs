using AutoMapper;
using hotel_erp.Api.Dtos.Common;
using hotel_erp.Api.Services.Interfaces;
using hotel_erp.Api.Database.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace hotel_erp.Api.Controllers
{
    [ApiController]
    [Route("api/settings")]
    [Authorize]
    public class SettingsController : ControllerBase
    {
        private readonly IBusinessSettingsRepository _repo;
        private readonly IMapper _mapper;

        public SettingsController(IBusinessSettingsRepository repo, IMapper mapper)
        {
            _repo = repo;
            _mapper = mapper;
        }

        [HttpGet("business")]
        public async Task<ActionResult<BusinessSettingsDto>> Get()
        {
            var settings = await _repo.GetAsync() ?? new BusinessSettings();
            return Ok(_mapper.Map<BusinessSettingsDto>(settings));
        }

        [HttpPut("business")]
        public async Task<ActionResult<BusinessSettingsDto>> Update([FromBody] UpdateBusinessSettingsRequest request)
        {
            var existing = await _repo.GetAsync() ?? new BusinessSettings();

            if (request.BusinessName != null) existing.BusinessName = request.BusinessName;
            if (request.RTN != null) existing.RTN = request.RTN;
            if (request.Address != null) existing.Address = request.Address;
            if (request.Phone != null) existing.Phone = request.Phone;
            if (request.Email != null) existing.Email = request.Email;
            if (request.LogoBase64 != null) existing.LogoBase64 = request.LogoBase64;
            if (request.Footer != null) existing.Footer = request.Footer;
            if (request.IsvRate.HasValue) existing.IsvRate = request.IsvRate.Value;
            if (request.TouristTaxRate.HasValue) existing.TouristTaxRate = request.TouristTaxRate.Value;
            if (request.PrintPrinterName != null) existing.PrintPrinterName = request.PrintPrinterName;
            if (request.PrintWidth.HasValue) existing.PrintWidth = request.PrintWidth.Value;
            if (request.PrintLogoHeight.HasValue) existing.PrintLogoHeight = request.PrintLogoHeight.Value;
            if (request.PrintFontSize != null) existing.PrintFontSize = request.PrintFontSize;
            if (request.PrintLineSpacing.HasValue) existing.PrintLineSpacing = request.PrintLineSpacing.Value;
            if (request.ShowLogo.HasValue) existing.ShowLogo = request.ShowLogo.Value;
            if (request.ShowHeader.HasValue) existing.ShowHeader = request.ShowHeader.Value;
            if (request.ShowFiscal.HasValue) existing.ShowFiscal = request.ShowFiscal.Value;
            if (request.ShowGuest.HasValue) existing.ShowGuest = request.ShowGuest.Value;
            if (request.ShowItems.HasValue) existing.ShowItems = request.ShowItems.Value;
            if (request.ShowTotals.HasValue) existing.ShowTotals = request.ShowTotals.Value;
            if (request.ShowPayment.HasValue) existing.ShowPayment = request.ShowPayment.Value;
            if (request.ShowFooter.HasValue) existing.ShowFooter = request.ShowFooter.Value;
            if (request.HeaderAlign != null) existing.HeaderAlign = request.HeaderAlign;
            if (request.SeparatorChar != null) existing.SeparatorChar = request.SeparatorChar;
            if (request.MarginLeft.HasValue) existing.MarginLeft = request.MarginLeft.Value;

            await _repo.UpdateAsync(existing);
            return Ok(_mapper.Map<BusinessSettingsDto>(existing));
        }
    }
}

