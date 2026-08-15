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
            entity.ToTable("inventory_items");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.TenantId).HasColumnName("tenant_id");
            entity.Property(e => e.Mpn).HasColumnName("mpn").HasMaxLength(128);
            entity.Property(e => e.DistiSku).HasColumnName("disti_sku").HasMaxLength(256);
            entity.Property(e => e.Manufacturer).HasColumnName("manufacturer").HasMaxLength(256);
            entity.Property(e => e.Description).HasColumnName("description").HasMaxLength(2000);
            entity.Property(e => e.Stock).HasColumnName("stock");
            entity.Property(e => e.MinQty).HasColumnName("min_qty");
            entity.Property(e => e.ContainerType).HasColumnName("container_type").HasMaxLength(128);
            entity.Property(e => e.Region).HasColumnName("region").HasMaxLength(128);
            entity.Property(e => e.PriceTiersJson).HasColumnName("price_tiers_json").HasColumnType("text");
            entity.Property(e => e.Sku).HasColumnName("sku").HasMaxLength(128);
            entity.Property(e => e.Name).HasColumnName("name").HasMaxLength(256);
            entity.Property(e => e.Category).HasColumnName("category").HasMaxLength(128);
            entity.Property(e => e.QuantityOnHand).HasColumnName("quantity_on_hand");
            entity.Property(e => e.UnitPrice).HasColumnName("unit_price").HasPrecision(12, 2);
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");
            entity.Property(e => e.VendorId).HasColumnName("vendor_id");
            entity.Property(e => e.InboundDocumentId).HasColumnName("inbound_document_id");

            entity.HasIndex(e => new { e.TenantId, e.Sku }).IsUnique();
            entity.HasIndex(e => new { e.TenantId, e.Mpn });

            entity.HasOne(i => i.Vendor)
                .WithMany(v => v.InventoryItems)
                .HasForeignKey(i => i.VendorId)
                .OnDelete(DeleteBehavior.SetNull); // Unlinks vendor on inventory item deletion

            entity.HasOne(i => i.InboundDocument)
                .WithMany(d => d.InventoryItems)
                .HasForeignKey(i => i.InboundDocumentId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<Vendor>(entity =>
        {
            entity.HasIndex(e => new { e.TenantId, e.Email }).IsUnique();
        });

        modelBuilder.Entity<InboundDocument>(entity =>
        {
            entity.HasIndex(e => new { e.TenantId, e.FileName }).IsUnique();

            entity.HasOne(d => d.Vendor)
                .WithMany(v => v.InboundDocuments)
                .HasForeignKey(d => d.VendorId)
                .OnDelete(DeleteBehavior.SetNull); // Unlinks vendor from document when vendor is deleted
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