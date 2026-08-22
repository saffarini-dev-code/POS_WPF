using Microsoft.EntityFrameworkCore;
using POS_WPF.Domain.Catalog;
using POS_WPF.Domain.Inventory;
using POS_WPF.Domain.Units;

namespace POS_WPF.Infrastructure.Database;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Product> Products => Set<Product>();
    public DbSet<ProductUnit> ProductUnits => Set<ProductUnit>();
    public DbSet<InventoryTransaction> InventoryTransactions => Set<InventoryTransaction>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Product>(entity =>
        {
            entity.ToTable("Products");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Sku).HasMaxLength(100).IsRequired();
            entity.Property(x => x.Name).HasMaxLength(250).IsRequired();
            entity.Property(x => x.ArabicName).HasMaxLength(250);
            entity.HasIndex(x => x.Sku).IsUnique();
            entity.HasMany(x => x.Units)
                .WithOne()
                .HasForeignKey(x => x.ProductId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<ProductUnit>(entity =>
        {
            entity.ToTable("ProductUnits");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.UnitName).HasMaxLength(100).IsRequired();
            entity.Property(x => x.Abbreviation).HasMaxLength(20).IsRequired();
            entity.Property(x => x.ConversionFactorToBase).HasPrecision(18, 6).IsRequired();
            entity.Property(x => x.SellingPrice).HasPrecision(18, 4);
            entity.Property(x => x.PurchasePrice).HasPrecision(18, 4);
            entity.Property(x => x.Barcode).HasMaxLength(100);
            entity.HasIndex(x => new { x.ProductId, x.Abbreviation }).IsUnique();
            entity.HasIndex(x => x.Barcode).IsUnique().HasFilter("[Barcode] IS NOT NULL");
        });

        modelBuilder.Entity<InventoryTransaction>(entity =>
        {
            entity.ToTable("InventoryTransactions");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.TransactionQuantity).HasPrecision(18, 6).IsRequired();
            entity.Property(x => x.ConversionFactor).HasPrecision(18, 6).IsRequired();
            entity.Property(x => x.BaseQuantity).HasPrecision(18, 6).IsRequired();
            entity.Property(x => x.ReferenceType).HasMaxLength(50).IsRequired();
            entity.Property(x => x.Reason).HasMaxLength(500);
            entity.HasIndex(x => new { x.ProductId, x.OccurredAtUtc });
            entity.HasIndex(x => new { x.WarehouseId, x.OccurredAtUtc });
            entity.HasIndex(x => new { x.ReferenceType, x.ReferenceId });
        });
    }
}
