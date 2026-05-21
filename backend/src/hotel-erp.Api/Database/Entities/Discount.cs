using hotel_erp.Api.Database.Entities;
using hotel_erp.Api.Database.Entities;

namespace hotel_erp.Api.Database.Entities
{
    public class Discount : BaseEntity
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public DiscountType DiscountType { get; set; } = DiscountType.Porcentaje;
        public decimal Value { get; set; }
        public bool IsActive { get; set; } = true;
        public string? ApplicableTo { get; set; }
        public bool RequiresDocument { get; set; }
        public int? MinAge { get; set; }
        public int Priority { get; set; }
    }
}


