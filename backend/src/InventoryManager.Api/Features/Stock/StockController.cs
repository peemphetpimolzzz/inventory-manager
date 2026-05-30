using InventoryManager.Api.Common;
using InventoryManager.Api.Data;
using InventoryManager.Api.Domain;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace InventoryManager.Api.Features.Stock;

[ApiController]
[Route("api/products/{productId:int}")]
public class StockController(InventoryDbContext db, StockService stockService) : ControllerBase
{
    [HttpPost("stock-in")]
    public async Task<ActionResult<StockMovementResult>> StockIn(int productId, StockMovementRequest request)
    {
        var product = await stockService.ApplyMovementAsync(
            productId, TransactionType.In, request.Quantity, request.Note);
        return Ok(new StockMovementResult(product.Id, product.QuantityOnHand, product.IsLowStock));
    }

    [HttpPost("stock-out")]
    public async Task<ActionResult<StockMovementResult>> StockOut(int productId, StockMovementRequest request)
    {
        var product = await stockService.ApplyMovementAsync(
            productId, TransactionType.Out, request.Quantity, request.Note);
        return Ok(new StockMovementResult(product.Id, product.QuantityOnHand, product.IsLowStock));
    }

    [HttpGet("transactions")]
    public async Task<ActionResult<IReadOnlyList<StockTransactionDto>>> GetTransactions(int productId)
    {
        if (!await db.Products.AnyAsync(p => p.Id == productId))
        {
            throw new NotFoundException($"Product {productId} not found.");
        }

        var items = await db.StockTransactions.AsNoTracking()
            .Where(t => t.ProductId == productId)
            .OrderByDescending(t => t.CreatedAt)
            .Select(t => new StockTransactionDto(t.Id, t.ProductId, t.Product!.Name,
                t.Type.ToString(), t.Quantity, t.Note, t.CreatedAt))
            .ToListAsync();
        return Ok(items);
    }
}
