namespace InvenFlow.Core.Entities;

public class InventoryItem
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Sku { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public int QuantityOnHand { get; set; }
    public decimal UnitPrice { get; set; }
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}