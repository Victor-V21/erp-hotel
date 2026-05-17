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
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string DiscountType { get; set; } = "Porcentaje";
        public decimal Value { get; set; }
        public bool IsActive { get; set; } = true;
        public string? ApplicableTo { get; set; }
        public bool RequiresDocument { get; set; }
        public int? MinAge { get; set; }
        public int Priority { get; set; }
    }

    public record UpdateDiscountRequest
    {
        public string? Name { get; set; }
        public string? Description { get; set; }
        public string? DiscountType { get; set; }
        public decimal? Value { get; set; }
        public bool? IsActive { get; set; }
        public string? ApplicableTo { get; set; }
        public bool? RequiresDocument { get; set; }
        public int? MinAge { get; set; }
        public int? Priority { get; set; }
    }
}
