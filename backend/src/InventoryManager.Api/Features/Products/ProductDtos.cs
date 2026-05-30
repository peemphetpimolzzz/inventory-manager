using System.ComponentModel.DataAnnotations;

namespace InventoryManager.Api.Features.Products;

public record ProductDto(
    int Id, string Sku, string Name, int CategoryId, string CategoryName,
    decimal UnitPrice, int QuantityOnHand, int ReorderLevel, bool IsLowStock,
    DateTime CreatedAt, DateTime UpdatedAt);

public record CreateProductRequest(
    [Required, MaxLength(50)] string Sku,
    [Required, MaxLength(200)] string Name,
    [Range(1, int.MaxValue)] int CategoryId,
    [Range(0, double.MaxValue)] decimal UnitPrice,
    [Range(0, int.MaxValue)] int QuantityOnHand,
    [Range(0, int.MaxValue)] int ReorderLevel);

public record UpdateProductRequest(
    [Required, MaxLength(50)] string Sku,
    [Required, MaxLength(200)] string Name,
    [Range(1, int.MaxValue)] int CategoryId,
    [Range(0, double.MaxValue)] decimal UnitPrice,
    [Range(0, int.MaxValue)] int ReorderLevel);
