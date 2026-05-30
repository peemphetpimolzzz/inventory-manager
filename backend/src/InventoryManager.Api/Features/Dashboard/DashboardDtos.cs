namespace InventoryManager.Api.Features.Dashboard;

public record DashboardDto(
    int TotalProducts,
    int TotalCategories,
    decimal TotalStockValue,
    int LowStockCount,
    IReadOnlyList<RecentTransactionDto> RecentTransactions);

public record RecentTransactionDto(int Id, string ProductName, string Type, int Quantity, DateTime CreatedAt);
