using Microsoft.EntityFrameworkCore;
using POS_WPF.Domain.Audit;
using POS_WPF.Domain.Customers;
using POS_WPF.Domain.Finance;
using POS_WPF.Domain.Inventory;
using POS_WPF.Domain.Products;
using POS_WPF.Domain.Promotions;
using POS_WPF.Domain.Purchasing;
using POS_WPF.Domain.Returns;
using POS_WPF.Domain.Security;
using POS_WPF.Domain.Sales;
using POS_WPF.Domain.Settings;
using POS_WPF.Domain.Stores;
using POS_WPF.Domain.Sync;

namespace POS_WPF.Data;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Product> Products => Set<Product>();
    public DbSet<ProductUnit> ProductUnits => Set<ProductUnit>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<InventoryTransaction> InventoryTransactions => Set<InventoryTransaction>();
    public DbSet<User> Users => Set<User>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<UserRole> UserRoles => Set<UserRole>();
    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();
    public DbSet<Branch> Branches => Set<Branch>();
    public DbSet<Warehouse> Warehouses => Set<Warehouse>();
    public DbSet<Terminal> Terminals => Set<Terminal>();
    public DbSet<CashRegister> CashRegisters => Set<CashRegister>();
    public DbSet<Sale> Sales => Set<Sale>();
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Supplier> Suppliers => Set<Supplier>();
    public DbSet<Purchase> Purchases => Set<Purchase>();
    public DbSet<SalesReturn> SalesReturns => Set<SalesReturn>();
    public DbSet<AuditEntry> AuditEntries => Set<AuditEntry>();
    public DbSet<SyncQueueEntry> SyncQueueEntries => Set<SyncQueueEntry>();
    public DbSet<CashMovement> CashMovements => Set<CashMovement>();
    public DbSet<StoreSettings> StoreSettings => Set<StoreSettings>();
    public DbSet<InvoiceSettings> InvoiceSettings => Set<InvoiceSettings>();
    public DbSet<InvoiceFooterLine> InvoiceFooterLines => Set<InvoiceFooterLine>();
    public DbSet<TaxSettings> TaxSettings => Set<TaxSettings>();
    public DbSet<Promotion> Promotions => Set<Promotion>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Product>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Sku).HasMaxLength(64).IsRequired();
            entity.Property(x => x.Name).HasMaxLength(200).IsRequired();
            entity.Property(x => x.NameArabic).HasMaxLength(200);
            entity.HasIndex(x => x.Sku).IsUnique();
            entity.HasOne<ProductUnit>().WithMany().HasForeignKey(x => x.BaseUnitId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<Category>().WithMany().HasForeignKey(x => x.CategoryId).OnDelete(DeleteBehavior.Restrict);
            entity.HasMany(x => x.Units).WithOne(x => x.Product).HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Restrict);
        });
        modelBuilder.Entity<Category>(entity =>
        {
            entity.HasKey(x => x.Id); entity.Property(x => x.Name).HasMaxLength(150).IsRequired(); entity.Property(x => x.NameArabic).HasMaxLength(150);
            entity.HasIndex(x => new { x.ParentCategoryId, x.Name }).IsUnique();
        });
        modelBuilder.Entity<ProductUnit>(entity =>
        {
            entity.HasKey(x => x.Id); entity.Property(x => x.Name).HasMaxLength(100).IsRequired(); entity.Property(x => x.Abbreviation).HasMaxLength(20).IsRequired(); entity.Property(x => x.Barcode).HasMaxLength(100);
            entity.Property(x => x.ConversionFactorToBase).HasPrecision(18, 6); entity.Property(x => x.SellingPrice).HasPrecision(18, 2); entity.Property(x => x.PurchasePrice).HasPrecision(18, 2);
            entity.HasIndex(x => x.Barcode).IsUnique().HasFilter("Barcode IS NOT NULL"); entity.HasIndex(x => new { x.ProductId, x.Name }).IsUnique();
        });
        modelBuilder.Entity<InventoryTransaction>(entity =>
        {
            entity.HasKey(x => x.Id); entity.Property(x => x.TransactionQuantity).HasPrecision(18, 6); entity.Property(x => x.ConversionFactor).HasPrecision(18, 6); entity.Property(x => x.BaseQuantity).HasPrecision(18, 6); entity.Property(x => x.Reference).HasMaxLength(100);
            entity.HasIndex(x => new { x.ProductId, x.CreatedAt }); entity.HasIndex(x => new { x.WarehouseId, x.ProductId, x.CreatedAt });
        });
        modelBuilder.Entity<User>(entity => { entity.HasKey(x => x.Id); entity.Property(x => x.Username).HasMaxLength(100).IsRequired(); entity.Property(x => x.DisplayName).HasMaxLength(200).IsRequired(); entity.Property(x => x.PasswordHash).HasMaxLength(500).IsRequired(); entity.HasIndex(x => x.Username).IsUnique(); });
        modelBuilder.Entity<Role>(entity => { entity.HasKey(x => x.Id); entity.Property(x => x.Name).HasMaxLength(100).IsRequired(); entity.HasIndex(x => x.Name).IsUnique(); });
        modelBuilder.Entity<UserRole>(entity => entity.HasKey(x => new { x.UserId, x.RoleId }));
        modelBuilder.Entity<RolePermission>(entity => { entity.HasKey(x => x.PermissionId); entity.Property(x => x.PermissionCode).HasMaxLength(150).IsRequired(); entity.HasIndex(x => new { x.RoleId, x.PermissionCode }).IsUnique(); });
        modelBuilder.Entity<Branch>(entity => { entity.HasKey(x => x.Id); entity.Property(x => x.Code).HasMaxLength(50).IsRequired(); entity.Property(x => x.Name).HasMaxLength(200).IsRequired(); entity.HasIndex(x => x.Code).IsUnique(); });
        modelBuilder.Entity<Warehouse>(entity => { entity.HasKey(x => x.Id); entity.Property(x => x.Code).HasMaxLength(50).IsRequired(); entity.Property(x => x.Name).HasMaxLength(200).IsRequired(); entity.HasIndex(x => new { x.BranchId, x.Code }).IsUnique(); });
        modelBuilder.Entity<Terminal>(entity => { entity.HasKey(x => x.Id); entity.Property(x => x.Code).HasMaxLength(50).IsRequired(); entity.Property(x => x.Name).HasMaxLength(200).IsRequired(); entity.HasIndex(x => new { x.BranchId, x.Code }).IsUnique(); });
        modelBuilder.Entity<CashRegister>(entity => { entity.HasKey(x => x.Id); entity.Property(x => x.Code).HasMaxLength(50).IsRequired(); entity.Property(x => x.Name).HasMaxLength(200).IsRequired(); entity.HasIndex(x => new { x.BranchId, x.Code }).IsUnique(); entity.Property(x => x.OpeningBalance).HasPrecision(18, 2); });
        modelBuilder.Entity<Sale>(entity => { entity.HasKey(x => x.Id); entity.Property(x => x.Number).HasMaxLength(50).IsRequired(); entity.HasIndex(x => x.Number).IsUnique(); entity.Property(x => x.Subtotal).HasPrecision(18, 2); entity.Property(x => x.Discount).HasPrecision(18, 2); entity.Property(x => x.Tax).HasPrecision(18, 2); entity.Property(x => x.Total).HasPrecision(18, 2); entity.HasMany(x => x.Lines).WithOne().HasForeignKey("SaleId").OnDelete(DeleteBehavior.Cascade); entity.HasMany(x => x.Payments).WithOne().HasForeignKey("SaleId").OnDelete(DeleteBehavior.Cascade); });
        modelBuilder.Entity<SaleLine>(entity => { entity.HasKey(x => x.Id); entity.Property(x => x.Description).HasMaxLength(250).IsRequired(); entity.Property(x => x.Quantity).HasPrecision(18, 6); entity.Property(x => x.ConversionFactor).HasPrecision(18, 6); entity.Property(x => x.UnitPrice).HasPrecision(18, 4); entity.Property(x => x.Discount).HasPrecision(18, 2); entity.Property(x => x.Tax).HasPrecision(18, 2); });
        modelBuilder.Entity<SalePayment>(entity => { entity.HasKey(x => x.Id); entity.Property(x => x.Method).HasMaxLength(50).IsRequired(); entity.Property(x => x.Amount).HasPrecision(18, 2); });
        modelBuilder.Entity<Customer>(entity => { entity.HasKey(x => x.Id); entity.Property(x => x.Code).HasMaxLength(50).IsRequired(); entity.Property(x => x.Name).HasMaxLength(200).IsRequired(); entity.Property(x => x.CreditLimit).HasPrecision(18, 2); entity.Property(x => x.CurrentBalance).HasPrecision(18, 2); entity.HasIndex(x => x.Code).IsUnique(); });
        modelBuilder.Entity<Supplier>(entity => { entity.HasKey(x => x.Id); entity.Property(x => x.Code).HasMaxLength(50).IsRequired(); entity.Property(x => x.Name).HasMaxLength(200).IsRequired(); entity.Property(x => x.CurrentBalance).HasPrecision(18, 2); entity.HasIndex(x => x.Code).IsUnique(); });
        modelBuilder.Entity<Purchase>(entity => { entity.HasKey(x => x.Id); entity.Property(x => x.Number).HasMaxLength(50).IsRequired(); entity.HasIndex(x => x.Number).IsUnique(); entity.Property(x => x.Subtotal).HasPrecision(18, 2); entity.Property(x => x.Discount).HasPrecision(18, 2); entity.Property(x => x.Tax).HasPrecision(18, 2); entity.Property(x => x.Total).HasPrecision(18, 2); entity.HasMany(x => x.Lines).WithOne().HasForeignKey("PurchaseId").OnDelete(DeleteBehavior.Cascade); });
        modelBuilder.Entity<PurchaseLine>(entity => { entity.HasKey(x => x.Id); entity.Property(x => x.Quantity).HasPrecision(18, 6); entity.Property(x => x.ConversionFactor).HasPrecision(18, 6); entity.Property(x => x.UnitCost).HasPrecision(18, 4); entity.Property(x => x.Discount).HasPrecision(18, 2); entity.Property(x => x.Tax).HasPrecision(18, 2); });
        modelBuilder.Entity<SalesReturn>(entity => { entity.HasKey(x => x.Id); entity.Property(x => x.Number).HasMaxLength(50).IsRequired(); entity.HasIndex(x => x.Number).IsUnique(); entity.HasMany(x => x.Lines).WithOne().HasForeignKey("SalesReturnId").OnDelete(DeleteBehavior.Cascade); });
        modelBuilder.Entity<SalesReturnLine>(entity => { entity.HasKey(x => x.Id); entity.Property(x => x.Quantity).HasPrecision(18, 6); entity.Property(x => x.HistoricalConversionFactor).HasPrecision(18, 6); entity.Property(x => x.UnitPrice).HasPrecision(18, 4); });
        modelBuilder.Entity<CashMovement>(entity => { entity.HasKey(x => x.Id); entity.Property(x => x.Amount).HasPrecision(18, 2); entity.Property(x => x.Reference).HasMaxLength(100).IsRequired(); entity.HasIndex(x => new { x.RegisterId, x.CreatedAtUtc }); });
        modelBuilder.Entity<AuditEntry>(entity => { entity.HasKey(x => x.Id); entity.Property(x => x.Action).HasMaxLength(100).IsRequired(); entity.Property(x => x.EntityType).HasMaxLength(100).IsRequired(); entity.Property(x => x.OldValue).HasMaxLength(4000); entity.Property(x => x.NewValue).HasMaxLength(4000); entity.Property(x => x.Reason).HasMaxLength(1000); entity.Property(x => x.Details).HasMaxLength(4000); entity.HasIndex(x => new { x.EntityType, x.EntityId, x.CreatedAtUtc }); });
        modelBuilder.Entity<SyncQueueEntry>(entity => { entity.HasKey(x => x.Id); entity.Property(x => x.EntityType).HasMaxLength(100).IsRequired(); entity.Property(x => x.Operation).HasMaxLength(50).IsRequired(); entity.Property(x => x.Payload).IsRequired(); entity.Property(x => x.LastError).HasMaxLength(2000); entity.HasIndex(x => new { x.Status, x.CreatedAtUtc }); });
        modelBuilder.Entity<StoreSettings>(entity => { entity.HasKey(x => x.Id); entity.Property(x => x.StoreName).HasMaxLength(200).IsRequired(); entity.Property(x => x.CurrencyCode).HasMaxLength(10).IsRequired(); entity.Property(x => x.LogoPath).HasMaxLength(500); entity.HasIndex(x => x.BranchId).IsUnique(); });
        modelBuilder.Entity<InvoiceSettings>(entity => { entity.HasKey(x => x.Id); entity.Property(x => x.HeaderText).HasMaxLength(1000); entity.HasMany(x => x.FooterLines).WithOne().HasForeignKey("InvoiceSettingsId").OnDelete(DeleteBehavior.Cascade); });
        modelBuilder.Entity<InvoiceFooterLine>(entity => { entity.HasKey(x => x.Id); entity.Property(x => x.Text).HasMaxLength(500).IsRequired(); });
        modelBuilder.Entity<TaxSettings>(entity => { entity.HasKey(x => x.Id); entity.Property(x => x.Rate).HasPrecision(9, 4); });
        modelBuilder.Entity<Promotion>(entity => { entity.HasKey(x => x.Id); entity.Property(x => x.Code).HasMaxLength(50).IsRequired(); entity.Property(x => x.Name).HasMaxLength(200).IsRequired(); entity.HasIndex(x => x.Code).IsUnique(); entity.Property(x => x.Value).HasPrecision(18, 4); entity.Property(x => x.MinimumQuantity).HasPrecision(18, 6); entity.Property(x => x.RewardQuantity).HasPrecision(18, 6); });
    }
}
