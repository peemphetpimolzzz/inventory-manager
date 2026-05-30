using System.Net;
using System.Net.Http.Json;
using InventoryManager.Api.Features.Categories;
using InventoryManager.Api.Features.Dashboard;
using InventoryManager.Api.Features.Products;
using InventoryManager.Api.Features.Stock;
using Xunit;

namespace InventoryManager.IntegrationTests;

public class StockApiTests(CustomWebApplicationFactory factory)
    : IClassFixture<CustomWebApplicationFactory>, IAsyncLifetime
{
    private readonly HttpClient _client = factory.CreateClient();

    public Task InitializeAsync() => factory.ResetDatabaseAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    private async Task<ProductDto> SetupProductAsync(int quantity, int reorderLevel)
    {
        var categoryResponse = await _client.PostAsJsonAsync("/api/categories",
            new SaveCategoryRequest("Stock Cat", null));
        var category = (await categoryResponse.Content.ReadFromJsonAsync<CategoryDto>())!;

        var productResponse = await _client.PostAsJsonAsync("/api/products",
            new CreateProductRequest("STK-1", "Item", category.Id, 10m, quantity, reorderLevel));
        return (await productResponse.Content.ReadFromJsonAsync<ProductDto>())!;
    }

    [Fact]
    public async Task StockIn_increases_quantity()
    {
        var product = await SetupProductAsync(10, 5);
        var response = await _client.PostAsJsonAsync(
            $"/api/products/{product.Id}/stock-in", new StockMovementRequest(7, "restock"));

        response.EnsureSuccessStatusCode();
        var result = (await response.Content.ReadFromJsonAsync<StockMovementResult>())!;
        Assert.Equal(17, result.QuantityOnHand);
    }

    [Fact]
    public async Task StockOut_below_zero_is_rejected_and_quantity_unchanged()
    {
        var product = await SetupProductAsync(3, 5);
        var response = await _client.PostAsJsonAsync(
            $"/api/products/{product.Id}/stock-out", new StockMovementRequest(5, null));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);

        var after = await _client.GetFromJsonAsync<ProductDto>($"/api/products/{product.Id}");
        Assert.Equal(3, after!.QuantityOnHand);
    }

    [Fact]
    public async Task Movements_are_recorded_and_dashboard_reflects_low_stock()
    {
        var product = await SetupProductAsync(10, 5);
        await _client.PostAsJsonAsync(
            $"/api/products/{product.Id}/stock-out", new StockMovementRequest(7, "issue"));

        var transactions = await _client.GetFromJsonAsync<List<StockTransactionDto>>(
            $"/api/products/{product.Id}/transactions");
        Assert.Single(transactions!);
        Assert.Equal("Out", transactions![0].Type);

        var dashboard = await _client.GetFromJsonAsync<DashboardDto>("/api/dashboard");
        Assert.True(dashboard!.LowStockCount >= 1);
    }
}
