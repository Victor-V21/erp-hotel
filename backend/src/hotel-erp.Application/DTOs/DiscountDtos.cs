using System.ComponentModel.DataAnnotations;

namespace hotel_erp.Application.DTOs
{
    public record DiscountDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string DiscountType { get; set; } = "Porcentaje";
        public decimal Value { get; set; }
        public bool IsActive { get; set; }
        public string? ApplicableTo { get; set; }
        public bool RequiresDocument { get; set; }
        public int? MinAge { get; set; }
        public int Priority { get; set; }
    }

    public record CreateDiscountRequest
    {
        [Required]
        [StringLength(100, MinimumLength = 2)]
        public string Name { get; set; } = string.Empty;
        [StringLength(250)]
        public string? Description { get; set; }
        [RegularExpression("^(Porcentaje|MontoFijo)$")]
        public string DiscountType { get; set; } = "Porcentaje";
        [Range(0.01, 999999.99)]
        public decimal Value { get; set; }
        public bool IsActive { get; set; } = true;
        [StringLength(50)]
        public string? ApplicableTo { get; set; }
        public bool RequiresDocument { get; set; }
        [Range(0, 130)]
        public int? MinAge { get; set; }
        [Range(0, 1000)]
        public int Priority { get; set; }
    }

    public record UpdateDiscountRequest
    {
        [StringLength(100, MinimumLength = 2)]
        public string? Name { get; set; }
        [StringLength(250)]
        public string? Description { get; set; }
        [RegularExpression("^(Porcentaje|MontoFijo)$")]
        public string? DiscountType { get; set; }
        [Range(0.01, 999999.99)]
        public decimal? Value { get; set; }
        public bool? IsActive { get; set; }
        [StringLength(50)]
        public string? ApplicableTo { get; set; }
        public bool? RequiresDocument { get; set; }
        [Range(0, 130)]
        public int? MinAge { get; set; }
        [Range(0, 1000)]
        public int? Priority { get; set; }
    }
}
