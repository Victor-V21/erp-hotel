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
            => await _context.BusinessSettings.FirstOrDefaultAsync();

        public async Task UpdateAsync(BusinessSettings settings)
        {
            var existing = await _context.BusinessSettings.FirstOrDefaultAsync();
            if (existing != null)
            {
                existing.BusinessName = settings.BusinessName;
                existing.RTN = settings.RTN;
                existing.Address = settings.Address;
                existing.Phone = settings.Phone;
                existing.Email = settings.Email;
                existing.LogoBase64 = settings.LogoBase64;
                existing.Footer = settings.Footer;
            }
            else
            {
                await _context.BusinessSettings.AddAsync(settings);
            }
            await _context.SaveChangesAsync();
        }
    }
}

