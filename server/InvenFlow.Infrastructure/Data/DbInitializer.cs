using System.Text.Json;
using InvenFlow.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace InvenFlow.Infrastructure.Data;

public static class DbInitializer
{
    public static async Task InitializeAsync(AppDbContext dbContext, ILogger logger, CancellationToken cancellationToken = default)
    {
        await dbContext.Database.MigrateAsync(cancellationToken);

        var tenant = await EnsureDefaultTenantAsync(dbContext, cancellationToken);
        await SeedBav99InventoryAsync(dbContext, tenant.Id, logger, cancellationToken);
    }

    private static async Task<Tenant> EnsureDefaultTenantAsync(AppDbContext dbContext, CancellationToken cancellationToken)
    {
        var tenant = await dbContext.Tenants
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(t => t.Slug == "invenflow-hq", cancellationToken);

        if (tenant is not null)
        {
            return tenant;
        }

        tenant = new Tenant
        {
            Name = "InvenFlow HQ",
            Slug = "invenflow-hq",
            CreatedAt = DateTime.UtcNow
        };

        dbContext.Tenants.Add(tenant);
        await dbContext.SaveChangesAsync(cancellationToken);

        return tenant;
    }

    private static async Task SeedBav99InventoryAsync(AppDbContext dbContext, Guid tenantId, ILogger logger, CancellationToken cancellationToken)
    {
        var hasBav99 = await dbContext.InventoryItems
            .IgnoreQueryFilters()
            .AnyAsync(i => i.TenantId == tenantId && EF.Functions.ILike(i.Mpn, "%BAV99%"), cancellationToken);

        if (hasBav99)
        {
            return;
        }

        var seedItems = new[]
        {
            new InventoryItem
            {
                TenantId = tenantId,
                Mpn = "BAV99S,115",
                DistiSku = "DISTI # FC-BAV99S115-LOCAL",
                Manufacturer = "Nexperia",
                Description = "In-house qualified dual switching diode for standard protection and signal applications.",
                Stock = 45000,
                MinQty = 1,
                ContainerType = "Cut Tape",
                Region = "In-House",
                PriceTiersJson = JsonSerializer.Serialize(new[]
                {
                    new { Qty = 1, UnitPrice = 0.067m },
                    new { Qty = 100, UnitPrice = 0.041m },
                    new { Qty = 1000, UnitPrice = 0.029m }
                }),
                Sku = "BAV99S,115",
                Name = "BAV99S,115 In-House Switching Diode",
                Category = "Electronics",
                QuantityOnHand = 45000,
                UnitPrice = 0.067m,
                UpdatedAt = DateTime.UtcNow
            },
            new InventoryItem
            {
                TenantId = tenantId,
                Mpn = "BAV99,235",
                DistiSku = "DISTI # FC-BAV99235-LOCAL",
                Manufacturer = "Nexperia",
                Description = "Compact high-speed dual diode for automotive-grade clamp and switching circuits.",
                Stock = 28000,
                MinQty = 1,
                ContainerType = "Tape & Reel",
                Region = "In-House",
                PriceTiersJson = JsonSerializer.Serialize(new[]
                {
                    new { Qty = 1, UnitPrice = 0.069m },
                    new { Qty = 100, UnitPrice = 0.042m },
                    new { Qty = 1000, UnitPrice = 0.031m }
                }),
                Sku = "BAV99,235",
                Name = "BAV99,235 In-House Switching Diode",
                Category = "Electronics",
                QuantityOnHand = 28000,
                UnitPrice = 0.069m,
                UpdatedAt = DateTime.UtcNow
            },
            new InventoryItem
            {
                TenantId = tenantId,
                Mpn = "BAV99-E3-18",
                DistiSku = "DISTI # FC-BAV99E318-LOCAL",
                Manufacturer = "Vishay",
                Description = "In-house stocked Vishay reel variant for board-level routing and high-speed switching.",
                Stock = 17350,
                MinQty = 10,
                ContainerType = "Vishay Reel",
                Region = "In-House",
                PriceTiersJson = JsonSerializer.Serialize(new[]
                {
                    new { Qty = 10, UnitPrice = 0.071m },
                    new { Qty = 250, UnitPrice = 0.048m },
                    new { Qty = 1000, UnitPrice = 0.035m }
                }),
                Sku = "BAV99-E3-18",
                Name = "BAV99-E3-18 In-House Small Signal Diode",
                Category = "Electronics",
                QuantityOnHand = 17350,
                UnitPrice = 0.071m,
                UpdatedAt = DateTime.UtcNow
            }
        };

        await dbContext.InventoryItems.AddRangeAsync(seedItems, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Seeded {Count} BAV99 inventory records for tenant {TenantId}", seedItems.Length, tenantId);
    }
}
