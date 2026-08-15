using InvenFlow.Api.Application.DTOs;
using InvenFlow.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace InvenFlow.Api.Application.Search;

public class FetchchipsLocalAdapter : IProductAdapter
{
    private readonly AppDbContext _context;

    public FetchchipsLocalAdapter(AppDbContext context)
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

        var matchPattern = $"%{normalizedQuery}%";

        // Flexible local matching keeps Fetchchips rows available for broad query terms.
        var items = await _context.InventoryItems
            .IgnoreQueryFilters()
            .Include(i => i.Vendor)
            .Where(i => EF.Functions.ILike(i.Mpn, matchPattern)
                || EF.Functions.ILike(i.Description, matchPattern)
                || EF.Functions.ILike(i.DistiSku, matchPattern)
                || EF.Functions.ILike(i.Category, matchPattern))
            .OrderByDescending(i => i.Stock > 0 ? i.Stock : i.QuantityOnHand)
            .ToListAsync();

        var mapped = items.Select(i => new UniversalProductDto
        {
            ProviderName = "Fetchchips Direct",
            DistiSku = string.IsNullOrWhiteSpace(i.DistiSku) ? $"DISTI # FC-{i.Sku}" : i.DistiSku,
            PartNumber = string.IsNullOrWhiteSpace(i.Mpn) ? i.Sku : i.Mpn,
            PackagingContainer = string.IsNullOrWhiteSpace(i.ContainerType) ? "In-House Reel" : i.ContainerType,
            MinQty = i.MinQty > 0 ? i.MinQty : 1,
            RegionStock = string.IsNullOrWhiteSpace(i.Region) ? $"In-House - {(i.Stock > 0 ? i.Stock : i.QuantityOnHand)}" : $"{i.Region} - {(i.Stock > 0 ? i.Stock : i.QuantityOnHand)}",
            RoHSStatus = "Compliant",
            LeadTime = (i.Stock > 0 ? i.Stock : i.QuantityOnHand) > 0 ? "Ready Stock" : "45 Weeks",
            ItemId = i.Id.ToString(),
            Category = string.IsNullOrWhiteSpace(i.Category) ? CategoryDomain : i.Category,
            Title = string.IsNullOrWhiteSpace(i.Name) ? i.Mpn : i.Name,
            BrandOrManufacturer = string.IsNullOrWhiteSpace(i.Manufacturer) ? (i.Vendor?.Name ?? "Fetchchips") : i.Manufacturer,
            SKU = string.IsNullOrWhiteSpace(i.Mpn) ? i.Sku : i.Mpn,
            Description = string.IsNullOrWhiteSpace(i.Description) ? string.Format("{0} | Fetchchips In-House Stock", i.Name) : i.Description,
            PublicSupplierName = "Fetchchips Direct",
            SupplierRealId = $"FETCHCHIPS:{i.Id}",
            DirectPurchaseUrl = $"https://invenflow.local/orders/new?inventoryItemId={i.Id}&sku={Uri.EscapeDataString(string.IsNullOrWhiteSpace(i.Sku) ? i.Mpn : i.Sku)}",
            VendorCartId = $"FETCHCHIPS:{i.Id}",
            AvailableStock = i.Stock > 0 ? i.Stock : i.QuantityOnHand,
            AvailabilityStatus = (i.Stock > 0 ? i.Stock : i.QuantityOnHand) > 0 ? "In Stock - In-House" : "Backorder - Lead time on request",
            Currency = "USD",
            PriceBreaks = BuildPriceTiers(i.PriceTiersJson, i.UnitPrice),
            Attributes = new Dictionary<string, string>
            {
                ["Source"] = "FetchchipsLocal",
                ["InventoryItemId"] = i.Id.ToString(),
                ["Category"] = i.Category,
                ["UpdatedAtUtc"] = i.UpdatedAt.ToString("O")
            }
        }).ToList();

        return mapped;
    }

    private static List<PriceTierDto> BuildPriceTiers(string? priceTiersJson, decimal fallbackUnitPrice)
    {
        if (!string.IsNullOrWhiteSpace(priceTiersJson))
        {
            try
            {
                var parsed = System.Text.Json.JsonSerializer.Deserialize<List<PriceTierDto>>(priceTiersJson);
                if (parsed is not null && parsed.Count > 0)
                {
                    return parsed;
                }
            }
            catch
            {
                // Fall back to single-tier pricing when the json payload is malformed.
            }
        }

        return new List<PriceTierDto>
        {
            new() { Qty = 1, UnitPrice = fallbackUnitPrice }
        };
    }
}
