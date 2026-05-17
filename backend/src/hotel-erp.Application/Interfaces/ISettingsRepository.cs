using hotel_erp.Domain.Entities;

namespace hotel_erp.Application.Interfaces
{
    public interface IBusinessSettingsRepository
    {
        Task<BusinessSettings?> GetAsync();
        Task UpdateAsync(BusinessSettings settings);
    }
}
