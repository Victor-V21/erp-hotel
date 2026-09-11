using Microsoft.EntityFrameworkCore;
using hotel_erp.Api.Services.Interfaces;
using hotel_erp.Api.Database.Entities;

namespace hotel_erp.Api.Database.Repositories
{
    public class BusinessSettingsRepository : IBusinessSettingsRepository
    {
        private readonly ApplicationDbContext _context;
        public BusinessSettingsRepository(ApplicationDbContext context) => _context = context;

        public async Task<BusinessSettings?> GetAsync()
            => await _context.BusinessSettings
                .OrderBy(settings => settings.CreatedAt)
                .ThenBy(settings => settings.Id)
                .FirstOrDefaultAsync();

        public async Task UpdateAsync(BusinessSettings settings)
        {
            var existing = await _context.BusinessSettings
                .OrderBy(candidate => candidate.CreatedAt)
                .ThenBy(candidate => candidate.Id)
                .FirstOrDefaultAsync();
            if (existing != null)
            {
                existing.BusinessName = settings.BusinessName;
                existing.RTN = settings.RTN;
                existing.Address = settings.Address;
                existing.Phone = settings.Phone;
                existing.Email = settings.Email;
                existing.LogoBase64 = settings.LogoBase64;
                existing.Footer = settings.Footer;
                existing.IsvRate = settings.IsvRate;
                existing.TouristTaxRate = settings.TouristTaxRate;
                existing.FiscalProfileStatus = settings.FiscalProfileStatus;
                existing.FiscalProfileVersion = settings.FiscalProfileVersion;
                existing.FiscalValidFrom = settings.FiscalValidFrom;
                existing.FiscalValidUntil = settings.FiscalValidUntil;
                existing.FiscalApprovedAt = settings.FiscalApprovedAt;
                existing.FiscalApprovedByUserId = settings.FiscalApprovedByUserId;
                existing.FiscalApprovalNote = settings.FiscalApprovalNote;
                existing.FiscalRetiredAt = settings.FiscalRetiredAt;
                existing.FiscalRetiredByUserId = settings.FiscalRetiredByUserId;
                existing.FiscalRetirementReason = settings.FiscalRetirementReason;
                existing.PrintPrinterName = settings.PrintPrinterName;
                existing.PrintWidth = settings.PrintWidth;
                existing.PrintLogoHeight = settings.PrintLogoHeight;
                existing.PrintFontSize = settings.PrintFontSize;
                existing.PrintLineSpacing = settings.PrintLineSpacing;
                existing.ShowLogo = settings.ShowLogo;
                existing.ShowHeader = settings.ShowHeader;
                existing.ShowFiscal = settings.ShowFiscal;
                existing.ShowGuest = settings.ShowGuest;
                existing.ShowItems = settings.ShowItems;
                existing.ShowTotals = settings.ShowTotals;
                existing.ShowPayment = settings.ShowPayment;
                existing.ShowFooter = settings.ShowFooter;
                existing.HeaderAlign = settings.HeaderAlign;
                existing.SeparatorChar = settings.SeparatorChar;
                existing.MarginLeft = settings.MarginLeft;
            }
            else
            {
                await _context.BusinessSettings.AddAsync(settings);
            }
            await _context.SaveChangesAsync();
        }
    }
}
