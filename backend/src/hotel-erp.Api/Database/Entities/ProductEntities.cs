namespace hotel_erp.Api.Database.Entities
{
    public class Product : BaseEntity
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? SKU { get; set; }
        public Guid? CategoryId { get; set; }
        public Category? Category { get; set; }
        public decimal UnitPrice { get; set; }
        public int CurrentStock { get; set; }
        public int MinStockLevel { get; set; }
        public bool IsActive { get; set; } = true;

        public ICollection<InventoryMovement> InventoryMovements { get; set; } = new List<InventoryMovement>();
    }

    public class Category : BaseEntity
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }

        public ICollection<Product> Products { get; set; } = new List<Product>();
    }

    public class InventoryMovement : BaseEntity
    {
        public Guid ProductId { get; set; }
        public Product Product { get; set; } = null!;
        public InventoryMovementType MovementType { get; set; }
        public int Quantity { get; set; }
        public DateTime MovementDate { get; set; } = DateTime.UtcNow;
        public decimal? UnitPrice { get; set; }
        public decimal? TotalValue { get; set; }
        public int NewStock { get; set; }
        public Guid? ReferenceId { get; set; }
        public Guid? UserId { get; set; }
        public User? User { get; set; }
    }
}


