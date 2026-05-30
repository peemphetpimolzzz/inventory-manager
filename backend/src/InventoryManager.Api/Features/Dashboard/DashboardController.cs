using InventoryManager.Api.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace InventoryManager.Api.Features.Dashboard;

[ApiController]
[Route("api/dashboard")]
public class DashboardController(InventoryDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<DashboardDto>> Get()
    {
        var totalProducts = await db.Products.CountAsync();
        var totalCategories = await db.Categories.CountAsync();
        var totalStockValue = await db.Products
            .SumAsync(p => (decimal?)(p.UnitPrice * p.QuantityOnHand)) ?? 0m;
        var lowStockCount = await db.Products.CountAsync(p => p.QuantityOnHand <= p.ReorderLevel);

        var recent = await db.StockTransactions.AsNoTracking()
            .OrderByDescending(t => t.CreatedAt)
            .Take(8)
            .Select(t => new RecentTransactionDto(
                t.Id, t.Product!.Name, t.Type.ToString(), t.Quantity, t.CreatedAt))
            .ToListAsync();

        return Ok(new DashboardDto(totalProducts, totalCategories, totalStockValue, lowStockCount, recent));
    }
}
