using InventoryManager.Api.Common;

namespace InventoryManager.Api.Domain;

/// <summary>
/// Pure stock rules, kept free of any database concern so they can be unit-tested directly.
/// </summary>
public static class StockRules
{
    public static int ApplyDelta(int currentQuantity, TransactionType type, int quantity)
    {
        if (quantity <= 0)
        {
            throw new BusinessRuleException("Quantity must be greater than zero.");
        }

        if (type == TransactionType.Out && quantity > currentQuantity)
        {
            throw new BusinessRuleException(
                $"Cannot remove {quantity} units; only {currentQuantity} on hand.");
        }

        return type == TransactionType.In ? currentQuantity + quantity : currentQuantity - quantity;
    }

    public static bool IsLowStock(int quantityOnHand, int reorderLevel) => quantityOnHand <= reorderLevel;
}
