using InventoryManager.Api.Common;
using InventoryManager.Api.Data;
using InventoryManager.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace InventoryManager.Api.Features.Stock;

public class StockService(InventoryDbContext db)
{
    /// <summary>
    /// Applies a stock movement and records it in the ledger atomically: the product
    /// quantity and the transaction row are committed together, or not at all.
    /// </summary>
    public async Task<Product> ApplyMovementAsync(int productId, TransactionType type, int quantity, string? note)
    {
        await using var tx = await db.Database.BeginTransactionAsync();

        var product = await db.Products.FirstOrDefaultAsync(p => p.Id == productId)
            ?? throw new NotFoundException($"Product {productId} not found.");

        product.QuantityOnHand = StockRules.ApplyDelta(product.QuantityOnHand, type, quantity);
        product.UpdatedAt = DateTime.UtcNow;

        db.StockTransactions.Add(new StockTransaction
        {
            ProductId = productId,
            Type = type,
            Quantity = quantity,
            Note = note,
            CreatedAt = DateTime.UtcNow,
        });

        await db.SaveChangesAsync();
        await tx.CommitAsync();
        return product;
    }
}
