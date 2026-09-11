using AutoMapper;
using hotel_erp.Api.Authorization;
using hotel_erp.Api.Dtos.Common;
using hotel_erp.Api.Services.Interfaces;
using hotel_erp.Api.Database.Entities;
using hotel_erp.Api.Services;
using hotel_erp.Api.Database;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace hotel_erp.Api.Controllers
{
    [ApiController]
    [Route("api/settings")]
    [Authorize]
    public class SettingsController : ControllerBase
    {
        private readonly IBusinessSettingsRepository _repo;
        private readonly IMapper _mapper;
        private readonly AuditService _auditService;
        private readonly ApplicationDbContext _context;

        public SettingsController(IBusinessSettingsRepository repo, IMapper mapper, AuditService auditService, ApplicationDbContext context)
        {
            _repo = repo;
            _mapper = mapper;
            _auditService = auditService;
            _context = context;
        }

        [HttpGet("business")]
        public async Task<ActionResult<BusinessSettingsDto>> Get()
        {
            var settings = await _repo.GetAsync() ?? new BusinessSettings();
            return Ok(_mapper.Map<BusinessSettingsDto>(settings));
        }

        [HttpPut("business")]
        [Authorize(Policy = PermissionNames.ManageSettings)]
        public async Task<ActionResult<BusinessSettingsDto>> Update([FromBody] UpdateBusinessSettingsRequest request)
        {
            var existing = await _repo.GetAsync() ?? new BusinessSettings();
            await using var transaction = await _context.Database.BeginTransactionAsync();

            var fiscalChanged =
                (request.BusinessName != null && request.BusinessName.Trim() != existing.BusinessName)
                || (request.RTN != null && request.RTN.Trim() != existing.RTN)
                || (request.Address != null && request.Address.Trim() != existing.Address)
                || (request.IsvRate.HasValue && request.IsvRate.Value != existing.IsvRate)
                || (request.TouristTaxRate.HasValue && request.TouristTaxRate.Value != existing.TouristTaxRate)
                || (request.FiscalValidFrom.HasValue && request.FiscalValidFrom != existing.FiscalValidFrom)
                || (request.FiscalValidUntil.HasValue && request.FiscalValidUntil != existing.FiscalValidUntil);

            if (request.BusinessName != null) existing.BusinessName = request.BusinessName.Trim();
            if (request.RTN != null) existing.RTN = request.RTN.Trim();
            if (request.Address != null) existing.Address = request.Address.Trim();
            if (request.Phone != null) existing.Phone = request.Phone.Trim();
            if (request.Email != null) existing.Email = request.Email.Trim().ToLowerInvariant();
            if (request.LogoBase64 != null) existing.LogoBase64 = request.LogoBase64;
            if (request.Footer != null) existing.Footer = request.Footer;
            if (request.IsvRate.HasValue) existing.IsvRate = request.IsvRate.Value;
            if (request.TouristTaxRate.HasValue) existing.TouristTaxRate = request.TouristTaxRate.Value;
            if (request.FiscalValidFrom.HasValue) existing.FiscalValidFrom = request.FiscalValidFrom;
            if (request.FiscalValidUntil.HasValue) existing.FiscalValidUntil = request.FiscalValidUntil;
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

            if (fiscalChanged)
            {
                existing.FiscalProfileStatus = FiscalProfileStatus.Borrador;
                existing.FiscalProfileVersion++;
                existing.FiscalApprovedAt = null;
                existing.FiscalApprovedByUserId = null;
                existing.FiscalApprovalNote = null;
                existing.FiscalRetiredAt = null;
                existing.FiscalRetiredByUserId = null;
                existing.FiscalRetirementReason = null;
            }

            await _repo.UpdateAsync(existing);
            var actorId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            await _auditService.LogAsync(actorId, "UpdateBusinessSettings", nameof(BusinessSettings), existing.Id, new
            {
                existing.FiscalProfileStatus,
                existing.FiscalProfileVersion,
                FiscalDataChanged = fiscalChanged
            });
            await transaction.CommitAsync();
            return Ok(_mapper.Map<BusinessSettingsDto>(existing));
        }

        [HttpPost("business/approve")]
        [Authorize(Policy = PermissionNames.ManageTaxes)]
        public async Task<ActionResult<BusinessSettingsDto>> ApproveFiscalProfile([FromBody] ApproveFiscalProfileRequest request)
        {
            var settings = await _repo.GetAsync();
            if (settings is null)
                return BadRequest(new { message = "Primero debe guardar la configuración del negocio" });

            await using var transaction = await _context.Database.BeginTransactionAsync();

            settings.FiscalValidFrom = request.ValidFrom;
            settings.FiscalValidUntil = request.ValidUntil;
            var validationErrors = FiscalProfileService.ValidateForApproval(settings);
            if (validationErrors.Count > 0)
                return BadRequest(new { message = "El perfil fiscal está incompleto", errors = validationErrors });

            var actorId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            settings.FiscalProfileStatus = FiscalProfileStatus.Aprobado;
            settings.FiscalApprovedAt = DateTime.UtcNow;
            settings.FiscalApprovedByUserId = actorId;
            settings.FiscalApprovalNote = request.ApprovalNote.Trim();
            settings.FiscalRetiredAt = null;
            settings.FiscalRetiredByUserId = null;
            settings.FiscalRetirementReason = null;
            await _repo.UpdateAsync(settings);
            await _auditService.LogAsync(actorId, "ApproveFiscalProfile", nameof(BusinessSettings), settings.Id, new
            {
                settings.FiscalProfileVersion,
                settings.FiscalValidFrom,
                settings.FiscalValidUntil,
                settings.FiscalApprovalNote
            });
            await transaction.CommitAsync();
            return Ok(_mapper.Map<BusinessSettingsDto>(settings));
        }

        [HttpPost("business/retire")]
        [Authorize(Policy = PermissionNames.ManageTaxes)]
        public async Task<ActionResult<BusinessSettingsDto>> RetireFiscalProfile([FromBody] RetireFiscalProfileRequest request)
        {
            var settings = await _repo.GetAsync();
            if (settings is null)
                return NotFound();

            await using var transaction = await _context.Database.BeginTransactionAsync();

            var actorId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            settings.FiscalProfileStatus = FiscalProfileStatus.Retirado;
            settings.FiscalRetiredAt = DateTime.UtcNow;
            settings.FiscalRetiredByUserId = actorId;
            settings.FiscalRetirementReason = request.Reason.Trim();
            await _repo.UpdateAsync(settings);
            await _auditService.LogAsync(actorId, "RetireFiscalProfile", nameof(BusinessSettings), settings.Id, new
            {
                settings.FiscalProfileVersion,
                settings.FiscalRetirementReason
            });
            await transaction.CommitAsync();
            return Ok(_mapper.Map<BusinessSettingsDto>(settings));
        }
    }
}
