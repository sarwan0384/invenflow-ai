namespace InvenFlow.Core.Entities;

public class ExternalLinkSnapshot : ITenantOwned
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TenantId { get; set; }
    public string Url { get; set; } = string.Empty;
    public string LastHash { get; set; } = string.Empty;
    public DateTime LastCheckedAt { get; set; } = DateTime.UtcNow;
    public string? LastPayload { get; set; }
}
