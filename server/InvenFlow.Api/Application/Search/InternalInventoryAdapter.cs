using InvenFlow.Api.Application.DTOs;
using InvenFlow.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace InvenFlow.Api.Application.Search;

public class InternalInventoryAdapter : ISearchProviderAdapter
{
    private readonly AppDbContext _context;

    public InternalInventoryAdapter(AppDbContext context)
    {
        _context = context;
    }

    public string CategoryDomain => "Electronics";

    public async Task<List<UniversalProductDto>> SearchAsync(string query)
    {
        var normalizedQuery = query?.Trim();
        if (string.IsNullOrWhiteSpace(normalizedQuery))
        {
            return new List<UniversalProductDto>();
        }

        var items = await _context.InventoryItems
            .Include(i => i.Vendor)
            .Where(i => EF.Functions.ILike(i.Sku, $"%{normalizedQuery}%") || EF.Functions.ILike(i.Name, $"%{normalizedQuery}%"))
            .OrderByDescending(i => i.QuantityOnHand)
            .Take(50)
            .ToListAsync();

        return items.Select(i => new UniversalProductDto
        {
            ItemId = i.Id.ToString(),
            Category = string.IsNullOrWhiteSpace(i.Category) ? CategoryDomain : i.Category,
            Title = i.Name,
            BrandOrManufacturer = i.Vendor?.Name ?? "InvenFlow In-House",
            SKU = i.Sku,
            Description = $"{i.Name} | In-House Stock",
            PublicSupplierName = "In-House Stock",
            SupplierRealId = $"INTERNAL:{i.Id}",
            AvailableStock = i.QuantityOnHand,
            AvailabilityStatus = i.QuantityOnHand > 0 ? "In Stock - Ships in 24h" : "Backorder - Lead time on request",
            Currency = "USD",
            PriceBreaks = new List<PriceTierDto>
            {
                new() { Qty = 1, UnitPrice = i.UnitPrice }
            },
            Attributes = new Dictionary<string, string>
            {
                ["Source"] = "InternalInventory",
                ["InventoryItemId"] = i.Id.ToString(),
                ["Category"] = i.Category,
                ["UpdatedAtUtc"] = i.UpdatedAt.ToString("O")
            }
        }).ToList();
    }
}
