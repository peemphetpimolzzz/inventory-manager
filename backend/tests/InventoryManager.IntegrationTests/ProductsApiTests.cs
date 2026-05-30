using System.Net;
using System.Net.Http.Json;
using InventoryManager.Api.Common;
using InventoryManager.Api.Features.Categories;
using InventoryManager.Api.Features.Products;
using Xunit;

namespace InventoryManager.IntegrationTests;

public class ProductsApiTests(CustomWebApplicationFactory factory)
    : IClassFixture<CustomWebApplicationFactory>, IAsyncLifetime
{
    private readonly HttpClient _client = factory.CreateClient();

    public Task InitializeAsync() => factory.ResetDatabaseAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    private async Task<CategoryDto> CreateCategoryAsync(string name)
    {
        var response = await _client.PostAsJsonAsync("/api/categories", new SaveCategoryRequest(name, null));
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<CategoryDto>())!;
    }

    [Fact]
    public async Task Create_then_get_product_roundtrips()
    {
        var category = await CreateCategoryAsync("Test Cat");
        var create = await _client.PostAsJsonAsync("/api/products",
            new CreateProductRequest("SKU-1", "Widget", category.Id, 9.99m, 50, 10));

        Assert.Equal(HttpStatusCode.Created, create.StatusCode);
        var created = (await create.Content.ReadFromJsonAsync<ProductDto>())!;

        var fetched = await _client.GetFromJsonAsync<ProductDto>($"/api/products/{created.Id}");
        Assert.Equal("SKU-1", fetched!.Sku);
        Assert.Equal("Test Cat", fetched.CategoryName);
        Assert.False(fetched.IsLowStock);
    }

    [Fact]
    public async Task Duplicate_sku_is_rejected()
    {
        var category = await CreateCategoryAsync("Cat A");
        await _client.PostAsJsonAsync("/api/products",
            new CreateProductRequest("DUP", "First", category.Id, 1m, 1, 1));
        var second = await _client.PostAsJsonAsync("/api/products",
            new CreateProductRequest("DUP", "Second", category.Id, 1m, 1, 1));

        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
    }

    [Fact]
    public async Task Unknown_product_returns_404()
    {
        var response = await _client.GetAsync("/api/products/999999");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task LowStock_filter_returns_only_low_items()
    {
        var category = await CreateCategoryAsync("Cat B");
        await _client.PostAsJsonAsync("/api/products",
            new CreateProductRequest("LOW", "Low item", category.Id, 1m, 2, 5));
        await _client.PostAsJsonAsync("/api/products",
            new CreateProductRequest("OK", "Healthy item", category.Id, 1m, 100, 5));

        var page = await _client.GetFromJsonAsync<PagedResult<ProductDto>>(
            "/api/products?lowStockOnly=true");

        Assert.NotNull(page);
        Assert.All(page!.Items, p => Assert.True(p.IsLowStock));
        Assert.Contains(page.Items, p => p.Sku == "LOW");
        Assert.DoesNotContain(page.Items, p => p.Sku == "OK");
    }
}
