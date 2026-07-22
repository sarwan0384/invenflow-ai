namespace InvenFlow.Core.Entities;

public enum DocumentStatus
{
    Uploaded,
    Processing,
    ReviewRequired,
    Processed,
    Failed
}

public class InboundDocument
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string FileName { get; set; } = string.Empty;
    public string FileUrl { get; set; } = string.Empty;
    public DocumentStatus Status { get; set; } = DocumentStatus.Uploaded;
    public Guid? VendorId { get; set; }
    public Vendor? Vendor { get; set; }
    public string? RawAiJson { get; set; }
    public decimal ConfidenceScore { get; set; }
    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
}
