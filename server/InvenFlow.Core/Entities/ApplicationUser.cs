using Microsoft.AspNetCore.Identity;

namespace InvenFlow.Core.Entities;

public class ApplicationUser : IdentityUser<Guid>, ITenantOwned
{
    public Guid TenantId { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string? RefreshToken { get; set; }
    public DateTime? RefreshTokenExpiresAt { get; set; }
}
