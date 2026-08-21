using Microsoft.EntityFrameworkCore;
using POS_WPF.Domain.Products;
using POS_WPF.Domain.Inventory;

namespace POS_WPF.Data;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Product> Products => Set<Product>();
    public DbSet<ProductUnit> ProductUnits => Set<ProductUnit>();
    public DbSet<InventoryTransaction> InventoryTransactions => Set<InventoryTransaction>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Product>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Name).HasMaxLength(200).IsRequired();
            entity.Property(x => x.NameArabic).HasMaxLength(200);
            entity.Property(x => x.Sku).HasMaxLength(64).IsRequired();
            entity.HasIndex(x => x.Sku).IsUnique();
            entity.HasOne<ProductUnit>()
                .WithMany()
                .HasForeignKey(x => x.BaseUnitId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasMany(x => x.Units)
                .WithOne(x => x.Product)
                .HasForeignKey(x => x.ProductId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<ProductUnit>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Name).HasMaxLength(100).IsRequired();
            entity.Property(x => x.Abbreviation).HasMaxLength(20).IsRequired();
            entity.Property(x => x.Barcode).HasMaxLength(100);
            entity.Property(x => x.ConversionFactorToBase).HasPrecision(18, 6);
            entity.Property(x => x.SellingPrice).HasPrecision(18, 2);
            entity.Property(x => x.PurchasePrice).HasPrecision(18, 2);
            entity.HasIndex(x => x.Barcode).IsUnique().HasFilter("Barcode IS NOT NULL");
            entity.HasIndex(x => new { x.ProductId, x.Name }).IsUnique();
        });

        modelBuilder.Entity<InventoryTransaction>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.TransactionQuantity).HasPrecision(18, 6);
            entity.Property(x => x.ConversionFactor).HasPrecision(18, 6);
            entity.Property(x => x.BaseQuantity).HasPrecision(18, 6);
            entity.Property(x => x.Reference).HasMaxLength(100);
            entity.HasIndex(x => new { x.ProductId, x.CreatedAt });
            entity.HasIndex(x => new { x.WarehouseId, x.ProductId, x.CreatedAt });
        });
    }
}
