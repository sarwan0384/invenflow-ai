namespace InvenFlow.Api.Application.DTOs;

public class UniversalProductDto
{
    public string ProviderName { get; set; } = string.Empty;
    public string DistiSku { get; set; } = string.Empty;
    public string PartNumber { get; set; } = string.Empty;
    public string PackagingContainer { get; set; } = string.Empty;
    public int MinQty { get; set; }
    public string RegionStock { get; set; } = string.Empty;
    public string RoHSStatus { get; set; } = string.Empty;
    public string LeadTime { get; set; } = string.Empty;
    public string ItemId { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string BrandOrManufacturer { get; set; } = string.Empty;
    public string SKU { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string PublicSupplierName { get; set; } = string.Empty;
    public string SupplierRealId { get; set; } = string.Empty;
    public string DirectPurchaseUrl { get; set; } = string.Empty;
    public string VendorCartId { get; set; } = string.Empty;
    public int AvailableStock { get; set; }
    public string AvailabilityStatus { get; set; } = string.Empty;
    public string Currency { get; set; } = string.Empty;
    public List<PriceTierDto> PriceBreaks { get; set; } = new();
    public Dictionary<string, string> Attributes { get; set; } = new();
}
