using InventoryManager.Api.Common;
using InventoryManager.Api.Domain;
using Xunit;

namespace InventoryManager.UnitTests;

public class StockRulesTests
{
    [Fact]
    public void StockIn_increases_quantity()
    {
        Assert.Equal(15, StockRules.ApplyDelta(10, TransactionType.In, 5));
    }

    [Fact]
    public void StockOut_decreases_quantity()
    {
        Assert.Equal(4, StockRules.ApplyDelta(10, TransactionType.Out, 6));
    }

    [Fact]
    public void StockOut_below_zero_is_rejected()
    {
        var ex = Assert.Throws<BusinessRuleException>(
            () => StockRules.ApplyDelta(3, TransactionType.Out, 5));
        Assert.Contains("only 3 on hand", ex.Message);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-2)]
    public void NonPositive_quantity_is_rejected(int quantity)
    {
        Assert.Throws<BusinessRuleException>(
            () => StockRules.ApplyDelta(10, TransactionType.In, quantity));
    }

    [Theory]
    [InlineData(5, 10, true)]
    [InlineData(10, 10, true)]
    [InlineData(11, 10, false)]
    public void IsLowStock_uses_reorder_threshold(int onHand, int reorder, bool expected)
    {
        Assert.Equal(expected, StockRules.IsLowStock(onHand, reorder));
    }
}
