using System.ComponentModel.DataAnnotations;

namespace InventoryManager.Api.Features.Stock;

public record StockMovementRequest(
    [Range(1, int.MaxValue)] int Quantity,
    [MaxLength(500)] string? Note);

public record StockMovementResult(int ProductId, int QuantityOnHand, bool IsLowStock);

public record StockTransactionDto(
    int Id, int ProductId, string ProductName, string Type, int Quantity, string? Note, DateTime CreatedAt);
