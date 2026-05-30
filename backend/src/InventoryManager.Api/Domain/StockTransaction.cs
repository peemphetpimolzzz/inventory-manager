namespace InventoryManager.Api.Domain;

public class StockTransaction
{
    public int Id { get; set; }

    public int ProductId { get; set; }
    public Product? Product { get; set; }

    public TransactionType Type { get; set; }
    public int Quantity { get; set; }
    public string? Note { get; set; }
    public DateTime CreatedAt { get; set; }
}
