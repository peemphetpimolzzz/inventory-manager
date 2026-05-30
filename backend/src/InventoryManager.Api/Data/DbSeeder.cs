using InventoryManager.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace InventoryManager.Api.Data;

/// <summary>
/// Populates the database with realistic demo data on first run. Idempotent:
/// if any categories already exist the seeder does nothing, so it is safe to
/// run on every startup.
/// </summary>
public static class DbSeeder
{
    public static async Task SeedAsync(InventoryDbContext db)
    {
        if (await db.Categories.AnyAsync())
        {
            return;
        }

        var seededAt = new DateTime(2026, 1, 5, 9, 0, 0, DateTimeKind.Utc);

        var categories = new[]
        {
            new Category { Name = "Beverages", Description = "Soft drinks, coffee, tea, juices" },
            new Category { Name = "Stationery", Description = "Office and writing supplies" },
            new Category { Name = "Electronics", Description = "Accessories and small devices" },
            new Category { Name = "Cleaning", Description = "Cleaning and sanitation supplies" },
            new Category { Name = "Packaging", Description = "Boxes, tape, and wrapping" },
        };
        db.Categories.AddRange(categories);
        await db.SaveChangesAsync();

        Category Cat(string name) => categories.First(c => c.Name == name);

        var products = new List<Product>
        {
            Make("BEV-001", "Arabica Coffee Beans 1kg", Cat("Beverages"), 420.00m, 35, 10),
            Make("BEV-002", "Green Tea Box (25 bags)", Cat("Beverages"), 95.00m, 8, 12),
            Make("BEV-003", "Sparkling Water 500ml", Cat("Beverages"), 18.00m, 240, 48),
            Make("BEV-004", "Orange Juice 1L", Cat("Beverages"), 55.00m, 5, 20),
            Make("STA-001", "Gel Pen 0.5mm (Black)", Cat("Stationery"), 12.00m, 500, 100),
            Make("STA-002", "A4 Copy Paper (ream)", Cat("Stationery"), 110.00m, 60, 25),
            Make("STA-003", "Sticky Notes 76x76mm", Cat("Stationery"), 28.00m, 14, 30),
            Make("STA-004", "Stapler (full strip)", Cat("Stationery"), 145.00m, 22, 8),
            Make("ELE-001", "USB-C Cable 1m", Cat("Electronics"), 89.00m, 75, 20),
            Make("ELE-002", "Wireless Mouse", Cat("Electronics"), 349.00m, 9, 10),
            Make("ELE-003", "65W USB-C Charger", Cat("Electronics"), 590.00m, 18, 6),
            Make("ELE-004", "HDMI Cable 2m", Cat("Electronics"), 159.00m, 3, 8),
            Make("ELE-005", "32GB USB Flash Drive", Cat("Electronics"), 199.00m, 44, 15),
            Make("CLN-001", "Multi-surface Spray 750ml", Cat("Cleaning"), 65.00m, 30, 12),
            Make("CLN-002", "Microfiber Cloth (3-pack)", Cat("Cleaning"), 49.00m, 11, 15),
            Make("CLN-003", "Hand Sanitizer 500ml", Cat("Cleaning"), 79.00m, 120, 24),
            Make("CLN-004", "Trash Bags (50pcs)", Cat("Cleaning"), 89.00m, 7, 10),
            Make("PKG-001", "Shipping Box (Medium)", Cat("Packaging"), 15.00m, 320, 100),
            Make("PKG-002", "Packing Tape 48mm", Cat("Packaging"), 22.00m, 85, 30),
            Make("PKG-003", "Bubble Wrap Roll 10m", Cat("Packaging"), 130.00m, 6, 10),
            Make("PKG-004", "Mailing Envelope A5", Cat("Packaging"), 4.50m, 900, 200),
            Make("BEV-005", "Instant Cocoa Mix", Cat("Beverages"), 145.00m, 26, 10),
            Make("STA-005", "Whiteboard Marker (4-set)", Cat("Stationery"), 99.00m, 40, 12),
            Make("ELE-006", "Laptop Stand (Aluminium)", Cat("Electronics"), 690.00m, 13, 5),
            Make("CLN-005", "Glass Cleaner 500ml", Cat("Cleaning"), 59.00m, 19, 10),
        };
        db.Products.AddRange(products);
        await db.SaveChangesAsync();

        // A handful of historical movements so the dashboard's "recent activity" is populated.
        Product Prod(string sku) => products.First(p => p.Sku == sku);
        var transactions = new[]
        {
            new StockTransaction { ProductId = Prod("BEV-003").Id, Type = TransactionType.In, Quantity = 96, Note = "Opening stock", CreatedAt = seededAt.AddDays(-9) },
            new StockTransaction { ProductId = Prod("STA-001").Id, Type = TransactionType.In, Quantity = 200, Note = "Bulk purchase", CreatedAt = seededAt.AddDays(-7) },
            new StockTransaction { ProductId = Prod("ELE-001").Id, Type = TransactionType.Out, Quantity = 25, Note = "Issued to project A", CreatedAt = seededAt.AddDays(-5) },
            new StockTransaction { ProductId = Prod("BEV-002").Id, Type = TransactionType.Out, Quantity = 14, Note = "Pantry restock", CreatedAt = seededAt.AddDays(-3) },
            new StockTransaction { ProductId = Prod("PKG-001").Id, Type = TransactionType.In, Quantity = 120, Note = "Supplier delivery", CreatedAt = seededAt.AddDays(-2) },
            new StockTransaction { ProductId = Prod("CLN-004").Id, Type = TransactionType.Out, Quantity = 18, Note = "Facilities request", CreatedAt = seededAt.AddDays(-1) },
        };
        db.StockTransactions.AddRange(transactions);
        await db.SaveChangesAsync();

        Product Make(string sku, string name, Category category, decimal price, int qty, int reorder) => new()
        {
            Sku = sku,
            Name = name,
            Category = category,
            UnitPrice = price,
            QuantityOnHand = qty,
            ReorderLevel = reorder,
            CreatedAt = seededAt,
            UpdatedAt = seededAt,
        };
    }
}
