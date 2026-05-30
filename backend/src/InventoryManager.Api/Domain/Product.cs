namespace InventoryManager.Api.Domain;

public class Product
{
    public int Id { get; set; }
    public string Sku { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;

    public int CategoryId { get; set; }
    public Category? Category { get; set; }

    public decimal UnitPrice { get; set; }
    public int QuantityOnHand { get; set; }
    public int ReorderLevel { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public ICollection<StockTransaction> Transactions { get; set; } = new List<StockTransaction>();

    public bool IsLowStock => QuantityOnHand <= ReorderLevel;
}
