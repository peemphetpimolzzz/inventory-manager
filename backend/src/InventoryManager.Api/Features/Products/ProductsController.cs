using InventoryManager.Api.Common;
using InventoryManager.Api.Data;
using InventoryManager.Api.Domain;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace InventoryManager.Api.Features.Products;

[ApiController]
[Route("api/products")]
public class ProductsController(InventoryDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PagedResult<ProductDto>>> GetProducts(
        [FromQuery] string? search,
        [FromQuery] bool lowStockOnly = false,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = db.Products.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(p => p.Name.Contains(search) || p.Sku.Contains(search));
        }
        if (lowStockOnly)
        {
            query = query.Where(p => p.QuantityOnHand <= p.ReorderLevel);
        }

        var total = await query.CountAsync();
        var items = await query
            .OrderBy(p => p.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(p => new ProductDto(p.Id, p.Sku, p.Name, p.CategoryId, p.Category!.Name,
                p.UnitPrice, p.QuantityOnHand, p.ReorderLevel, p.QuantityOnHand <= p.ReorderLevel,
                p.CreatedAt, p.UpdatedAt))
            .ToListAsync();

        return Ok(new PagedResult<ProductDto>(items, page, pageSize, total));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ProductDto>> GetById(int id)
    {
        var product = await Project(db, id).FirstOrDefaultAsync()
            ?? throw new NotFoundException($"Product {id} not found.");
        return Ok(product);
    }

    [HttpPost]
    public async Task<ActionResult<ProductDto>> Create(CreateProductRequest request)
    {
        if (!await db.Categories.AnyAsync(c => c.Id == request.CategoryId))
        {
            throw new BusinessRuleException($"Category {request.CategoryId} does not exist.");
        }
        if (await db.Products.AnyAsync(p => p.Sku == request.Sku))
        {
            throw new BusinessRuleException($"SKU '{request.Sku}' is already in use.");
        }

        var now = DateTime.UtcNow;
        var product = new Product
        {
            Sku = request.Sku,
            Name = request.Name,
            CategoryId = request.CategoryId,
            UnitPrice = request.UnitPrice,
            QuantityOnHand = request.QuantityOnHand,
            ReorderLevel = request.ReorderLevel,
            CreatedAt = now,
            UpdatedAt = now,
        };
        db.Products.Add(product);
        await db.SaveChangesAsync();
        return CreatedAtAction(nameof(GetById), new { id = product.Id },
            await Project(db, product.Id).FirstAsync());
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<ProductDto>> Update(int id, UpdateProductRequest request)
    {
        var product = await db.Products.FindAsync(id)
            ?? throw new NotFoundException($"Product {id} not found.");
        if (!await db.Categories.AnyAsync(c => c.Id == request.CategoryId))
        {
            throw new BusinessRuleException($"Category {request.CategoryId} does not exist.");
        }
        if (await db.Products.AnyAsync(p => p.Sku == request.Sku && p.Id != id))
        {
            throw new BusinessRuleException($"SKU '{request.Sku}' is already in use.");
        }

        product.Sku = request.Sku;
        product.Name = request.Name;
        product.CategoryId = request.CategoryId;
        product.UnitPrice = request.UnitPrice;
        product.ReorderLevel = request.ReorderLevel;
        product.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
        return Ok(await Project(db, id).FirstAsync());
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var product = await db.Products.FindAsync(id)
            ?? throw new NotFoundException($"Product {id} not found.");
        db.Products.Remove(product);
        await db.SaveChangesAsync();
        return NoContent();
    }

    private static IQueryable<ProductDto> Project(InventoryDbContext db, int id) =>
        db.Products.AsNoTracking()
            .Where(p => p.Id == id)
            .Select(p => new ProductDto(p.Id, p.Sku, p.Name, p.CategoryId, p.Category!.Name,
                p.UnitPrice, p.QuantityOnHand, p.ReorderLevel, p.QuantityOnHand <= p.ReorderLevel,
                p.CreatedAt, p.UpdatedAt));
}
