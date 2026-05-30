using InventoryManager.Api.Common;
using InventoryManager.Api.Data;
using InventoryManager.Api.Features.Stock;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new() { Title = "Inventory Manager API", Version = "v1" });
});

builder.Services.AddDbContext<InventoryDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("Default")));

builder.Services.AddScoped<StockService>();
builder.Services.AddHealthChecks();

builder.Services.AddCors(options => options.AddPolicy("dev", policy =>
    policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod()));

var app = builder.Build();

app.UseMiddleware<ApiExceptionMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseCors("dev");
}

// Swagger is exposed in every environment so the running container is self-documenting.
app.UseSwagger();
app.UseSwaggerUI(options => options.SwaggerEndpoint("/swagger/v1/swagger.json", "Inventory Manager API v1"));

app.MapControllers();
app.MapHealthChecks("/health");

await MigrateAndSeedAsync(app);

app.Run();

static async Task MigrateAndSeedAsync(WebApplication app)
{
    if (!app.Configuration.GetValue("RUN_MIGRATIONS", true))
    {
        return;
    }

    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<InventoryDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

    // SQL Server can report "healthy" a beat before it accepts logins, so retry the
    // first connection rather than crashing the container on a cold start.
    const int maxAttempts = 12;
    for (var attempt = 1; ; attempt++)
    {
        try
        {
            await db.Database.MigrateAsync();
            break;
        }
        catch (Exception ex) when (attempt < maxAttempts)
        {
            logger.LogWarning(ex, "Database not ready (attempt {Attempt}/{Max}); retrying in 5s.",
                attempt, maxAttempts);
            await Task.Delay(TimeSpan.FromSeconds(5));
        }
    }

    if (app.Configuration.GetValue("SEED_DATA", true))
    {
        await DbSeeder.SeedAsync(db);
    }
}

// Exposed so the integration test project can drive the app via WebApplicationFactory.
public partial class Program;
