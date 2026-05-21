using hotel_erp.Api.Database.Entities;

namespace hotel_erp.Api.Services.Interfaces
{
    public interface IBusinessSettingsRepository
    {
        Task<BusinessSettings?> GetAsync();
        Task UpdateAsync(BusinessSettings settings);
    }
}

