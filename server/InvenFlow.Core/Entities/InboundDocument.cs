namespace InvenFlow.Core.Entities;

public enum DocumentStatus
{
    Pending,
    Processing,
    Processed,
    Failed
}

public class InboundDocument : ITenantOwned
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public DocumentStatus Status { get; set; } = DocumentStatus.Pending;
    public double ConfidenceScore { get; set; }
    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;

    public Guid? VendorId { get; set; }
    public Vendor? Vendor { get; set; }
}