namespace InvenFlow.Api.Application.DTOs;

public class ProductOfferDto
{
    public string ProviderName { get; set; } = string.Empty;
    public string SupplierRealId { get; set; } = string.Empty;
    public string PartNumber { get; set; } = string.Empty;
    public string DistiSku { get; set; } = string.Empty;
    public int AvailableStock { get; set; }
    public string LeadTime { get; set; } = string.Empty;
    public int MinQty { get; set; }
    public int OrderMultiple { get; set; } = 1;
    public string PackagingContainer { get; set; } = string.Empty;
    public string Currency { get; set; } = "USD";
    public decimal BestUnitPrice { get; set; }
    public string DirectPurchaseUrl { get; set; } = string.Empty;
}

public class ProductDetailDto
{
    public string ProviderName { get; set; } = string.Empty;
    public string SupplierRealId { get; set; } = string.Empty;
    public string VendorCartId { get; set; } = string.Empty;
    public string Mpn { get; set; } = string.Empty;
    public string Manufacturer { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string DatasheetUrl { get; set; } = string.Empty;
    public string DirectPurchaseUrl { get; set; } = string.Empty;
    public int AvailableStock { get; set; }
    public string LeadTime { get; set; } = string.Empty;
    public int MinQty { get; set; }
    public int OrderMultiple { get; set; } = 1;
    public string PackagingContainer { get; set; } = string.Empty;
    public string Currency { get; set; } = "USD";
    public List<PriceTierDto> PriceBreaks { get; set; } = new();
    public Dictionary<string, string> Specifications { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    public List<ProductOfferDto> AlternateOffers { get; set; } = new();
}
