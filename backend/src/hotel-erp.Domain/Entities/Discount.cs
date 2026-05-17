using hotel_erp.Domain.Common;
using hotel_erp.Domain.Enums;

namespace hotel_erp.Domain.Entities
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
