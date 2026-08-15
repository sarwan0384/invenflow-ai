using System.Text.Json;
using InvenFlow.Api.Application.DTOs;
using InvenFlow.Api.Application.Search;
using InvenFlow.Core.Entities;
using InvenFlow.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace InvenFlow.Api.Application.ProductDetails;

public class DefaultProductDetailsProvider : IVendorDetailsProvider
{
    public string VendorKey => "DEFAULT";
    public bool IsEnabled => true;

    private readonly AggregatorSearchService _aggregatorSearchService;
    private readonly AppDbContext _dbContext;
    private readonly ILogger<DefaultProductDetailsProvider> _logger;

    public DefaultProductDetailsProvider(AggregatorSearchService aggregatorSearchService, AppDbContext dbContext, ILogger<DefaultProductDetailsProvider> logger)
    {
        _aggregatorSearchService = aggregatorSearchService;
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<ProductDetailDto> FetchDetailsAsync(string supplierRealId, string mpn, CancellationToken cancellationToken = default)
    {
        var normalizedMpn = (mpn ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(normalizedMpn))
        {
            return new ProductDetailDto();
        }

        var localPrimary = await ResolveLocalPrimaryOfferAsync(supplierRealId, normalizedMpn, cancellationToken);

        var grouped = await _aggregatorSearchService.SearchAsync(normalizedMpn, "Electronics");
        var allOffers = grouped.SelectMany(g => g.Results).ToList();

        if (localPrimary is not null && !allOffers.Any(x => string.Equals(x.SupplierRealId, localPrimary.SupplierRealId, StringComparison.OrdinalIgnoreCase)))
        {
            allOffers.Insert(0, localPrimary);
        }

        var primary = localPrimary ?? ResolvePrimaryOffer(allOffers, supplierRealId, normalizedMpn);

        if (primary is null)
        {
            _logger.LogInformation("No product detail candidates found for mpn {Mpn} and supplierRealId {SupplierRealId}", normalizedMpn, supplierRealId);
            return new ProductDetailDto
            {
                ProviderName = IsLocalSupplierRealId(supplierRealId) ? "Fetchchips Direct" : string.Empty,
                SupplierRealId = supplierRealId,
                Mpn = normalizedMpn,
                Description = "No product details were found for this selection.",
                DatasheetUrl = BuildDatasheetUrl(normalizedMpn)
            };
        }

        var alternateOffers = allOffers
            .Where(x => !string.Equals(x.SupplierRealId, primary.SupplierRealId, StringComparison.OrdinalIgnoreCase))
            .Select(MapOffer)
            .GroupBy(x => x.SupplierRealId, StringComparer.OrdinalIgnoreCase)
            .Select(g => g.First())
            .OrderByDescending(x => x.AvailableStock)
            .ThenBy(x => x.ProviderName)
            .ToList();

        var specs = BuildSpecs(primary);

        return new ProductDetailDto
        {
            ProviderName = primary.ProviderName,
            SupplierRealId = primary.SupplierRealId,
            VendorCartId = primary.VendorCartId,
            Mpn = string.IsNullOrWhiteSpace(primary.PartNumber) ? primary.SKU : primary.PartNumber,
            Manufacturer = primary.BrandOrManufacturer,
            Category = primary.Category,
            Description = primary.Description,
            DatasheetUrl = ResolveDatasheetUrl(primary),
            DirectPurchaseUrl = ResolvePurchaseUrl(primary),
            AvailableStock = primary.AvailableStock,
            LeadTime = primary.LeadTime,
            MinQty = primary.MinQty,
            OrderMultiple = ResolveOrderMultiple(primary),
            PackagingContainer = primary.PackagingContainer,
            Currency = string.IsNullOrWhiteSpace(primary.Currency) ? "USD" : primary.Currency,
            PriceBreaks = primary.PriceBreaks?.OrderBy(x => x.Qty).ToList() ?? new List<PriceTierDto>(),
            Specifications = specs,
            AlternateOffers = alternateOffers
        };
    }

    private async Task<UniversalProductDto?> ResolveLocalPrimaryOfferAsync(string supplierRealId, string mpn, CancellationToken cancellationToken)
    {
        var cleanQuery = mpn.Trim();
        var strippedIdentifier = StripVendorPrefix(supplierRealId);

        var query = _dbContext.InventoryItems
            .IgnoreQueryFilters()
            .Include(i => i.Vendor)
            .AsQueryable();

        query = query.Where(i => EF.Functions.ILike(i.Mpn, $"%{cleanQuery}%")
            || EF.Functions.ILike(i.Description, $"%{cleanQuery}%")
            || (!string.IsNullOrWhiteSpace(strippedIdentifier) && (
                EF.Functions.ILike(i.Mpn, $"%{strippedIdentifier}%")
                || EF.Functions.ILike(i.Sku, $"%{strippedIdentifier}%")
                || EF.Functions.ILike(i.Description, $"%{strippedIdentifier}%")
                || EF.Functions.ILike(i.DistiSku, $"%{strippedIdentifier}%"))));

        var item = await query
            .OrderByDescending(i => i.Stock > 0 ? i.Stock : i.QuantityOnHand)
            .FirstOrDefaultAsync(cancellationToken);

        return item is null ? null : MapLocalItem(item);
    }

    private static UniversalProductDto? ResolvePrimaryOffer(List<UniversalProductDto> offers, string supplierRealId, string mpn)
    {
        if (!string.IsNullOrWhiteSpace(supplierRealId))
        {
            var exact = offers.FirstOrDefault(x => string.Equals(x.SupplierRealId, supplierRealId, StringComparison.OrdinalIgnoreCase));
            if (exact is not null)
            {
                return exact;
            }
        }

        var normalizedMpn = Canonicalize(mpn);

        return offers
            .Where(x => Canonicalize(string.IsNullOrWhiteSpace(x.PartNumber) ? x.SKU : x.PartNumber).Contains(normalizedMpn, StringComparison.OrdinalIgnoreCase)
                || Canonicalize(x.SKU).Contains(normalizedMpn, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(x => x.AvailableStock)
            .ThenBy(x => x.ProviderName)
            .FirstOrDefault();
    }

    private static ProductOfferDto MapOffer(UniversalProductDto offer)
    {
        return new ProductOfferDto
        {
            ProviderName = offer.ProviderName,
            SupplierRealId = offer.SupplierRealId,
            PartNumber = string.IsNullOrWhiteSpace(offer.PartNumber) ? offer.SKU : offer.PartNumber,
            DistiSku = offer.DistiSku,
            AvailableStock = offer.AvailableStock,
            LeadTime = offer.LeadTime,
            MinQty = offer.MinQty,
            OrderMultiple = ResolveOrderMultiple(offer),
            PackagingContainer = offer.PackagingContainer,
            Currency = string.IsNullOrWhiteSpace(offer.Currency) ? "USD" : offer.Currency,
            BestUnitPrice = offer.PriceBreaks?.Where(x => x.Qty > 0).OrderBy(x => x.UnitPrice).Select(x => x.UnitPrice).FirstOrDefault() ?? 0m,
            DirectPurchaseUrl = ResolvePurchaseUrl(offer)
        };
    }

    private static int ResolveOrderMultiple(UniversalProductDto product)
    {
        if (product.Attributes is not null
            && product.Attributes.TryGetValue("OrderMultiple", out var orderMultipleValue)
            && int.TryParse(orderMultipleValue, out var parsedOrderMultiple)
            && parsedOrderMultiple > 0)
        {
            return parsedOrderMultiple;
        }

        return product.MinQty > 0 ? product.MinQty : 1;
    }

    private static Dictionary<string, string> BuildSpecs(UniversalProductDto product)
    {
        var specs = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        var attributes = product.Attributes ?? new Dictionary<string, string>();

        specs["Voltage"] = attributes.TryGetValue("Voltage", out var voltage) && !string.IsNullOrWhiteSpace(voltage) ? voltage : "N/A";
        specs["Current"] = attributes.TryGetValue("Current", out var current) && !string.IsNullOrWhiteSpace(current) ? current : "N/A";
        specs["Package"] = attributes.TryGetValue("Package", out var package) && !string.IsNullOrWhiteSpace(package)
            ? package
            : (string.IsNullOrWhiteSpace(product.PackagingContainer) ? "N/A" : product.PackagingContainer);
        specs["RoHS"] = string.IsNullOrWhiteSpace(product.RoHSStatus) ? "Unknown" : product.RoHSStatus;

        return specs;
    }

    private static string ResolveDatasheetUrl(UniversalProductDto product)
    {
        if (product.Attributes is not null
            && product.Attributes.TryGetValue("DatasheetUrl", out var datasheetUrl)
            && !string.IsNullOrWhiteSpace(datasheetUrl))
        {
            return datasheetUrl;
        }

        var mpn = string.IsNullOrWhiteSpace(product.PartNumber) ? product.SKU : product.PartNumber;
        return BuildDatasheetUrl(mpn);
    }

    private static string BuildDatasheetUrl(string mpn)
    {
        return $"https://www.google.com/search?q={Uri.EscapeDataString(mpn)}+datasheet";
    }

    private static string ResolvePurchaseUrl(UniversalProductDto product)
    {
        if (!string.IsNullOrWhiteSpace(product.DirectPurchaseUrl))
        {
            return product.DirectPurchaseUrl;
        }

        var part = Uri.EscapeDataString(string.IsNullOrWhiteSpace(product.PartNumber) ? product.SKU : product.PartNumber);
        var key = (product.SupplierRealId ?? string.Empty).Split(':', 2, StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
            .FirstOrDefault()
            ?.ToUpperInvariant() ?? string.Empty;

        return key switch
        {
            "ARROW" => $"https://www.arrow.com/en/buy/{part}",
            "DIGIKEY" => $"https://www.digikey.com/en/products/detail/-/{part}",
            "FETCHCHIPS" => $"https://invenflow.local/orders/new?sku={part}",
            "FETCH" => $"https://invenflow.local/orders/new?sku={part}",
            "LOCAL" => $"https://invenflow.local/orders/new?sku={part}",
            _ => string.Empty
        };
    }

    private static bool IsLocalSupplierRealId(string? supplierRealId)
    {
        if (string.IsNullOrWhiteSpace(supplierRealId))
        {
            return false;
        }

        var prefix = supplierRealId.Split(':', 2, StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
            .FirstOrDefault()
            ?.ToUpperInvariant() ?? string.Empty;

        return prefix is "FETCHCHIPS" or "FETCH" or "LOCAL";
    }

    private static string StripVendorPrefix(string? supplierRealId)
    {
        if (string.IsNullOrWhiteSpace(supplierRealId))
        {
            return string.Empty;
        }

        var parts = supplierRealId.Split(':', 2, StringSplitOptions.TrimEntries);
        return parts.Length == 2 ? parts[1] : parts[0];
    }

    private static UniversalProductDto MapLocalItem(InventoryItem item)
    {
        return new UniversalProductDto
        {
            ProviderName = "Fetchchips Direct",
            DistiSku = string.IsNullOrWhiteSpace(item.DistiSku) ? $"DISTI # FC-{item.Sku}" : item.DistiSku,
            PartNumber = string.IsNullOrWhiteSpace(item.Mpn) ? item.Sku : item.Mpn,
            PackagingContainer = string.IsNullOrWhiteSpace(item.ContainerType) ? "In-House Reel" : item.ContainerType,
            MinQty = item.MinQty > 0 ? item.MinQty : 1,
            RegionStock = string.IsNullOrWhiteSpace(item.Region) ? $"In-House - {(item.Stock > 0 ? item.Stock : item.QuantityOnHand)}" : $"{item.Region} - {(item.Stock > 0 ? item.Stock : item.QuantityOnHand)}",
            RoHSStatus = "Compliant",
            LeadTime = (item.Stock > 0 ? item.Stock : item.QuantityOnHand) > 0 ? "Ready Stock" : "45 Weeks",
            ItemId = item.Id.ToString(),
            Category = item.Category,
            Title = string.IsNullOrWhiteSpace(item.Name) ? item.Mpn : item.Name,
            BrandOrManufacturer = string.IsNullOrWhiteSpace(item.Manufacturer) ? (item.Vendor?.Name ?? "Fetchchips") : item.Manufacturer,
            SKU = string.IsNullOrWhiteSpace(item.Mpn) ? item.Sku : item.Mpn,
            Description = string.IsNullOrWhiteSpace(item.Description) ? string.Format("{0} | Fetchchips In-House Stock", item.Name) : item.Description,
            PublicSupplierName = "Fetchchips Direct",
            SupplierRealId = $"FETCHCHIPS:{item.Id}",
            DirectPurchaseUrl = $"https://invenflow.local/orders/new?inventoryItemId={item.Id}&sku={Uri.EscapeDataString(string.IsNullOrWhiteSpace(item.Sku) ? item.Mpn : item.Sku)}",
            VendorCartId = $"FETCHCHIPS:{item.Id}",
            AvailableStock = item.Stock > 0 ? item.Stock : item.QuantityOnHand,
            AvailabilityStatus = (item.Stock > 0 ? item.Stock : item.QuantityOnHand) > 0 ? "In Stock - In-House" : "Backorder - Lead time on request",
            Currency = "USD",
            PriceBreaks = BuildPriceTiers(item.PriceTiersJson, item.UnitPrice),
            Attributes = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["Source"] = "FetchchipsLocal",
                ["InventoryItemId"] = item.Id.ToString(),
                ["Category"] = item.Category,
                ["UpdatedAtUtc"] = item.UpdatedAt.ToString("O"),
                ["OrderMultiple"] = (item.MinQty > 0 ? item.MinQty : 1).ToString()
            }
        };
    }

    private static List<PriceTierDto> BuildPriceTiers(string? priceTiersJson, decimal fallbackUnitPrice)
    {
        if (!string.IsNullOrWhiteSpace(priceTiersJson))
        {
            try
            {
                var parsed = JsonSerializer.Deserialize<List<PriceTierDto>>(priceTiersJson);
                if (parsed is not null && parsed.Count > 0)
                {
                    return parsed.OrderBy(x => x.Qty).ToList();
                }
            }
            catch
            {
            }
        }

        return new List<PriceTierDto>
        {
            new() { Qty = 1, UnitPrice = fallbackUnitPrice }
        };
    }

    private static string Canonicalize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var chars = value.Trim().ToUpperInvariant().Where(char.IsLetterOrDigit).ToArray();
        return new string(chars);
    }
}
