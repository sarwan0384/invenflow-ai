namespace InvenFlow.Core.Entities;

public enum NotificationStatus
{
    PendingSync,
    Synced,
    Failed
}

public class Notification : ITenantOwned
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TenantId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string Category { get; set; } = "System";
    public string? PayloadJson { get; set; }
    public NotificationStatus Status { get; set; } = NotificationStatus.PendingSync;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public bool IsRead { get; set; }
}
