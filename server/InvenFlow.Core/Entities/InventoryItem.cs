namespace InvenFlow.Core.Entities;

public class InventoryItem : ITenantOwned
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TenantId { get; set; }
    public string Mpn { get; set; } = string.Empty;
    public string DistiSku { get; set; } = string.Empty;
    public string Manufacturer { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int Stock { get; set; }
    public int MinQty { get; set; }
    public string ContainerType { get; set; } = string.Empty;
    public string Region { get; set; } = string.Empty;
    public string PriceTiersJson { get; set; } = string.Empty;
    public string Sku { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public int QuantityOnHand { get; set; }
    public decimal UnitPrice { get; set; }
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Foreign keys for document-origin tracking
    public Guid? VendorId { get; set; }
    public Vendor? Vendor { get; set; }

    public Guid? InboundDocumentId { get; set; }
    public InboundDocument? InboundDocument { get; set; }
}
