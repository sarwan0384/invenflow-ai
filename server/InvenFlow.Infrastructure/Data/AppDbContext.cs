using System.Linq.Expressions;
using System.Reflection;
using InvenFlow.Core.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace InvenFlow.Infrastructure.Data;

public class AppDbContext : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>
{
    private readonly IHttpContextAccessor? _httpContextAccessor;

    public AppDbContext(DbContextOptions<AppDbContext> options, IHttpContextAccessor? httpContextAccessor = null) : base(options)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<Vendor> Vendors => Set<Vendor>();
    public DbSet<InventoryItem> InventoryItems => Set<InventoryItem>();
    public DbSet<InboundDocument> InboundDocuments => Set<InboundDocument>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<ExternalLinkSnapshot> ExternalLinkSnapshots => Set<ExternalLinkSnapshot>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Tenant>(entity =>
        {
            entity.HasIndex(e => e.Slug).IsUnique();
        });

        modelBuilder.Entity<InventoryItem>(entity =>
        {
            entity.HasIndex(e => new { e.TenantId, e.Sku }).IsUnique();
            entity.Property(e => e.UnitPrice).HasPrecision(12, 2);
        });

        modelBuilder.Entity<Vendor>(entity =>
        {
            entity.HasIndex(e => new { e.TenantId, e.Email }).IsUnique();
        });

        modelBuilder.Entity<InboundDocument>(entity =>
        {
            entity.HasIndex(e => new { e.TenantId, e.FileName }).IsUnique();
        });

        modelBuilder.Entity<ApplicationUser>(entity =>
        {
            entity.HasIndex(e => new { e.TenantId, e.UserName }).IsUnique();
        });

        var getCurrentTenantIdMethod = typeof(AppDbContext).GetMethod(nameof(GetCurrentTenantId), BindingFlags.Instance | BindingFlags.NonPublic);
        if (getCurrentTenantIdMethod == null)
        {
            return;
        }

        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (entityType.ClrType != typeof(Tenant) && typeof(ITenantOwned).IsAssignableFrom(entityType.ClrType))
            {
                var parameter = Expression.Parameter(entityType.ClrType, "e");
                var property = Expression.Property(parameter, nameof(ITenantOwned.TenantId));
                var propertyAsNullable = Expression.Convert(property, typeof(Guid?));
                var currentTenantIdCall = Expression.Call(Expression.Constant(this), getCurrentTenantIdMethod);
                var equality = Expression.Equal(propertyAsNullable, currentTenantIdCall);
                var lambda = Expression.Lambda(equality, parameter);
                entityType.SetQueryFilter(lambda);
            }
        }
    }

    private Guid? GetCurrentTenantId()
    {
        var tenantClaim = _httpContextAccessor?.HttpContext?.User.FindFirst("tenantId")?.Value
            ?? _httpContextAccessor?.HttpContext?.User.FindFirst("TenantId")?.Value;

        return Guid.TryParse(tenantClaim, out var tenantId) ? tenantId : null;
    }
}