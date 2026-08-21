namespace hotel_erp.Api.Dtos.Inventory
{
    public record CategoryDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
    }

    public record CreateCategoryRequest(string Name, string? Description = null);

    public record UpdateCategoryRequest(string? Name = null, string? Description = null);

    public record ProductDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? SKU { get; set; }
        public Guid? CategoryId { get; set; }
        public string? CategoryName { get; set; }
        public decimal UnitPrice { get; set; }
        public int CurrentStock { get; set; }
        public int MinStockLevel { get; set; }
        public bool IsActive { get; set; }
    }

    public record CreateProductRequest(
        string Name,
        string? Description,
        string? SKU,
        Guid? CategoryId,
        decimal UnitPrice,
        int CurrentStock,
        int MinStockLevel,
        bool IsActive = true);

    public record UpdateProductRequest(
        string? Name = null,
        string? Description = null,
        string? SKU = null,
        Guid? CategoryId = null,
        decimal? UnitPrice = null,
        int? CurrentStock = null,
        int? MinStockLevel = null,
        bool? IsActive = null);

    public record InventoryMovementDto
    {
        public Guid Id { get; set; }
        public Guid ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string MovementType { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public DateTime MovementDate { get; set; }
        public decimal? UnitPrice { get; set; }
        public decimal? TotalValue { get; set; }
        public int NewStock { get; set; }
        public Guid? ReferenceId { get; set; }
        public string? UserName { get; set; }
    }

    public record CreateInventoryMovementRequest(
        Guid ProductId,
        string MovementType,
        int Quantity,
        decimal? UnitPrice = null,
        Guid? ReferenceId = null,
        int? TargetStock = null);
}
