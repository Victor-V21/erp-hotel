using System.Security.Claims;
using hotel_erp.Api.Database;
using hotel_erp.Api.Database.Entities;
using hotel_erp.Api.Dtos.Inventory;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace hotel_erp.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class InventoryController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public InventoryController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet("categories")]
        public async Task<ActionResult<IEnumerable<CategoryDto>>> GetCategories()
            => Ok(await _context.Categories
                .OrderBy(c => c.Name)
                .Select(c => new CategoryDto { Id = c.Id, Name = c.Name, Description = c.Description })
                .ToListAsync());

        [HttpPost("categories")]
        public async Task<ActionResult<CategoryDto>> CreateCategory([FromBody] CreateCategoryRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Name)) return BadRequest("El nombre es obligatorio");
            var entity = new Category { Name = request.Name.Trim(), Description = request.Description };
            await _context.Categories.AddAsync(entity);
            await _context.SaveChangesAsync();
            return Ok(new CategoryDto { Id = entity.Id, Name = entity.Name, Description = entity.Description });
        }

        [HttpPut("categories/{id}")]
        public async Task<ActionResult> UpdateCategory(Guid id, [FromBody] UpdateCategoryRequest request)
        {
            var entity = await _context.Categories.FirstOrDefaultAsync(c => c.Id == id);
            if (entity == null) return NotFound();
            if (!string.IsNullOrWhiteSpace(request.Name)) entity.Name = request.Name.Trim();
            if (request.Description != null) entity.Description = request.Description;
            await _context.SaveChangesAsync();
            return NoContent();
        }

        [HttpDelete("categories/{id}")]
        public async Task<ActionResult> DeleteCategory(Guid id)
        {
            var entity = await _context.Categories.FirstOrDefaultAsync(c => c.Id == id);
            if (entity == null) return NotFound();
            entity.IsDeleted = true;
            await _context.SaveChangesAsync();
            return NoContent();
        }

        [HttpGet("products")]
        public async Task<ActionResult<IEnumerable<ProductDto>>> GetProducts([FromQuery] string? search)
        {
            var query = _context.Products.Include(p => p.Category).AsQueryable();
            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(p => p.Name.Contains(search) || (p.SKU != null && p.SKU.Contains(search)) || (p.Category != null && p.Category.Name.Contains(search)));
            }

            return Ok(await query
                .OrderBy(p => p.Name)
                .Select(p => new ProductDto
                {
                    Id = p.Id,
                    Name = p.Name,
                    Description = p.Description,
                    SKU = p.SKU,
                    CategoryId = p.CategoryId,
                    CategoryName = p.Category != null ? p.Category.Name : null,
                    UnitPrice = p.UnitPrice,
                    CurrentStock = p.CurrentStock,
                    MinStockLevel = p.MinStockLevel,
                    IsActive = p.IsActive
                })
                .ToListAsync());
        }

        [HttpPost("products")]
        public async Task<ActionResult<ProductDto>> CreateProduct([FromBody] CreateProductRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Name)) return BadRequest("El nombre es obligatorio");

            var entity = new Product
            {
                Name = request.Name.Trim(),
                Description = request.Description,
                SKU = request.SKU,
                CategoryId = request.CategoryId,
                UnitPrice = request.UnitPrice,
                CurrentStock = request.CurrentStock,
                MinStockLevel = request.MinStockLevel,
                IsActive = request.IsActive
            };

            await _context.Products.AddAsync(entity);
            await _context.SaveChangesAsync();
            return Ok(await MapProductAsync(entity.Id));
        }

        [HttpPut("products/{id}")]
        public async Task<ActionResult> UpdateProduct(Guid id, [FromBody] UpdateProductRequest request)
        {
            var entity = await _context.Products.FirstOrDefaultAsync(p => p.Id == id);
            if (entity == null) return NotFound();

            if (request.Name != null) entity.Name = request.Name.Trim();
            if (request.Description != null) entity.Description = request.Description;
            if (request.SKU != null) entity.SKU = request.SKU;
            if (request.CategoryId.HasValue) entity.CategoryId = request.CategoryId;
            if (request.UnitPrice.HasValue) entity.UnitPrice = request.UnitPrice.Value;
            if (request.CurrentStock.HasValue) entity.CurrentStock = request.CurrentStock.Value;
            if (request.MinStockLevel.HasValue) entity.MinStockLevel = request.MinStockLevel.Value;
            if (request.IsActive.HasValue) entity.IsActive = request.IsActive.Value;

            await _context.SaveChangesAsync();
            return NoContent();
        }

        [HttpDelete("products/{id}")]
        public async Task<ActionResult> DeleteProduct(Guid id)
        {
            var entity = await _context.Products.FirstOrDefaultAsync(p => p.Id == id);
            if (entity == null) return NotFound();
            entity.IsDeleted = true;
            await _context.SaveChangesAsync();
            return NoContent();
        }

        [HttpGet("movements")]
        public async Task<ActionResult<IEnumerable<InventoryMovementDto>>> GetMovements([FromQuery] Guid? productId)
        {
            var query = _context.InventoryMovements.Include(m => m.Product).Include(m => m.User).AsQueryable();
            if (productId.HasValue)
                query = query.Where(m => m.ProductId == productId.Value);

            return Ok(await query
                .OrderByDescending(m => m.MovementDate)
                .ThenByDescending(m => m.CreatedAt)
                .Select(m => new InventoryMovementDto
                {
                    Id = m.Id,
                    ProductId = m.ProductId,
                    ProductName = m.Product.Name,
                    MovementType = m.MovementType.ToString(),
                    Quantity = m.Quantity,
                    MovementDate = m.MovementDate,
                    UnitPrice = m.UnitPrice,
                    TotalValue = m.TotalValue,
                    NewStock = m.NewStock,
                    ReferenceId = m.ReferenceId,
                    UserName = m.User != null ? m.User.FirstName + " " + m.User.LastName : null
                })
                .ToListAsync());
        }

        [HttpPost("movements")]
        public async Task<ActionResult<InventoryMovementDto>> CreateMovement([FromBody] CreateInventoryMovementRequest request)
        {
            var product = await _context.Products.FirstOrDefaultAsync(p => p.Id == request.ProductId);
            if (product == null) return NotFound("Producto no encontrado");
            if (!Enum.TryParse<InventoryMovementType>(request.MovementType, out var movementType))
                return BadRequest("Tipo de movimiento inválido");
            if (request.Quantity <= 0) return BadRequest("La cantidad debe ser mayor que cero");

            var currentStock = product.CurrentStock;
            var newStock = movementType switch
            {
                InventoryMovementType.Entrada or InventoryMovementType.Compra => currentStock + request.Quantity,
                InventoryMovementType.Salida or InventoryMovementType.Venta => currentStock - request.Quantity,
                InventoryMovementType.Ajuste => request.TargetStock ?? currentStock,
                _ => currentStock
            };

            if (newStock < 0) return BadRequest("El inventario no puede quedar negativo");

            var effectiveQuantity = movementType == InventoryMovementType.Ajuste && request.TargetStock.HasValue
                ? Math.Abs(request.TargetStock.Value - currentStock)
                : request.Quantity;

            product.CurrentStock = newStock;

            var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var movement = new InventoryMovement
            {
                ProductId = product.Id,
                MovementType = movementType,
                Quantity = effectiveQuantity,
                UnitPrice = request.UnitPrice,
                TotalValue = request.UnitPrice.HasValue ? request.UnitPrice.Value * effectiveQuantity : null,
                NewStock = newStock,
                ReferenceId = request.ReferenceId,
                UserId = userId,
                MovementDate = DateTime.UtcNow
            };

            await _context.InventoryMovements.AddAsync(movement);
            await _context.SaveChangesAsync();
            return Ok(await MapMovementAsync(movement.Id));
        }

        private async Task<ProductDto?> MapProductAsync(Guid id)
            => await _context.Products
                .Include(p => p.Category)
                .Where(p => p.Id == id)
                .Select(p => new ProductDto
                {
                    Id = p.Id,
                    Name = p.Name,
                    Description = p.Description,
                    SKU = p.SKU,
                    CategoryId = p.CategoryId,
                    CategoryName = p.Category != null ? p.Category.Name : null,
                    UnitPrice = p.UnitPrice,
                    CurrentStock = p.CurrentStock,
                    MinStockLevel = p.MinStockLevel,
                    IsActive = p.IsActive
                })
                .FirstOrDefaultAsync();

        private async Task<InventoryMovementDto?> MapMovementAsync(Guid id)
            => await _context.InventoryMovements
                .Include(m => m.Product)
                .Include(m => m.User)
                .Where(m => m.Id == id)
                .Select(m => new InventoryMovementDto
                {
                    Id = m.Id,
                    ProductId = m.ProductId,
                    ProductName = m.Product.Name,
                    MovementType = m.MovementType.ToString(),
                    Quantity = m.Quantity,
                    MovementDate = m.MovementDate,
                    UnitPrice = m.UnitPrice,
                    TotalValue = m.TotalValue,
                    NewStock = m.NewStock,
                    ReferenceId = m.ReferenceId,
                    UserName = m.User != null ? m.User.FirstName + " " + m.User.LastName : null
                })
                .FirstOrDefaultAsync();
    }
}
