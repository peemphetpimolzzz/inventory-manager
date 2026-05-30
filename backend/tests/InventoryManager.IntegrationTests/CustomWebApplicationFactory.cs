using InventoryManager.Api.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace InventoryManager.IntegrationTests;

/// <summary>
/// Boots the real API in-process against the test SQL Server. Startup migration
/// and seeding are switched off; each test class recreates a clean schema.
/// </summary>
public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("RUN_MIGRATIONS", "false");
        builder.UseSetting("SEED_DATA", "false");
        builder.UseEnvironment("Production");
    }

    public async Task ResetDatabaseAsync()
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<InventoryDbContext>();
        await db.Database.EnsureDeletedAsync();
        await db.Database.MigrateAsync();
    }
}
