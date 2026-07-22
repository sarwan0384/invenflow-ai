using InvenFlow.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace InvenFlow.Infrastructure.Data;

// This class acts as our bridge manager to PostgreSQL
public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    // Drawer 1: Stores Company Vendors
    public DbSet<Vendor> Vendors => Set<Vendor>();

    // Drawer 2: Stores Warehouse Items
    public DbSet<InventoryItem> InventoryItems => Set<InventoryItem>();

    // Drawer 3: Stores Uploaded Receipt Files
    public DbSet<InboundDocument> InboundDocuments => Set<InboundDocument>();
}