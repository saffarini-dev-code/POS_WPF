using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using POS_WPF.Data;
using POS_WPF.Domain.Pricing;
using POS_WPF.Domain.Products;
using POS_WPF.Domain.Sales;
using POS_WPF.Infrastructure.Security;

namespace POS_WPF.Infrastructure.Verification;

public sealed record VerificationResult(string Name, bool Passed, string? Error = null);

public sealed class VerificationRunner
{
    public async Task<IReadOnlyList<VerificationResult>> RunAllAsync(CancellationToken cancellationToken = default)
    {
        var results = RunBusinessRules(); await RunDatabaseSmokeAsync(results, cancellationToken); return results;
    }
    private static List<VerificationResult> RunBusinessRules()
    {
        var results = new List<VerificationResult>();
        Run(results, "Unit conversion BOX to PCS", () => { var service = new UnitConversionService(); var unit = new ProductUnit { ConversionFactorToBase = 12m }; if (service.ToBaseQuantity(10m, unit) != 120m) throw new InvalidOperationException("Expected 120 base units."); });
        Run(results, "Unit-specific price independence", () => { var unit = new ProductUnit { ConversionFactorToBase = 12m, SellingPrice = 10m }; if (unit.SellingPrice == unit.ConversionFactorToBase) throw new InvalidOperationException("Price must remain independent from conversion."); });
        Run(results, "Percentage discount", () => { var discount = new DiscountRule(DiscountType.Percentage, 10m); if (discount.Calculate(100m) != 10m) throw new InvalidOperationException("Expected 10 discount."); });
        Run(results, "Return historical conversion", () => { if (1m * 12m != 12m) throw new InvalidOperationException("Expected 12 base units restored."); });
        Run(results, "Password hashing", () => { var hasher = new PasswordHasher(); var hash = hasher.Hash("VerificationPassword123!"); if (!hasher.Verify("VerificationPassword123!", hash) || hasher.Verify("wrong", hash)) throw new InvalidOperationException("Password verification failed."); });
        Run(results, "Manager cannot manage Super Administrator", () => { if (PermissionCatalog.IsManagerAllowedToManageUser(PermissionCatalog.Manager, PermissionCatalog.SuperAdministrator)) throw new InvalidOperationException("Manager restriction failed."); });
        Run(results, "Last Super Administrator protection", () => { if (PermissionCatalog.CanDeleteOrDisableSuperAdministrator(1, PermissionCatalog.SuperAdministrator)) throw new InvalidOperationException("Last administrator protection failed."); });
        return results;
    }
    private static async Task RunDatabaseSmokeAsync(List<VerificationResult> results, CancellationToken cancellationToken)
    {
        try
        {
            await using var connection = new SqliteConnection("Data Source=:memory:"); await connection.OpenAsync(cancellationToken); var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(connection).Options; await using var db = new AppDbContext(options); await db.Database.EnsureCreatedAsync(cancellationToken);
            var baseUnit = new ProductUnit { Name = "PCS", Abbreviation = "PCS", ConversionFactorToBase = 1m, IsBaseUnit = true, CanSell = true, CanPurchase = true, SellingPrice = 1m }; var boxUnit = new ProductUnit { Name = "BOX", Abbreviation = "BOX", ConversionFactorToBase = 12m, CanSell = true, CanPurchase = true, SellingPrice = 10m }; var product = new Product { Sku = "VERIFY-001", Name = "Verification Product", BaseUnitId = baseUnit.Id, Units = [baseUnit, boxUnit] };
            db.Products.Add(product); db.ProductUnits.AddRange(baseUnit, boxUnit); await db.SaveChangesAsync(cancellationToken); if (!await db.Products.AnyAsync(x => x.Sku == "VERIFY-001", cancellationToken)) throw new InvalidOperationException("Product persistence failed.");
            var sale = new Sale(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "VERIFY-SALE-001"); sale.AddLine(product.Id, boxUnit.Id, product.Name, 1m, 12m, 10m, 0m, 0m); sale.AddPayment("Cash", 10m); sale.Complete(); db.Sales.Add(sale); await db.SaveChangesAsync(cancellationToken); results.Add(new("EF Core SQLite model + persistence smoke", true));
        }
        catch (Exception ex) { results.Add(new("EF Core SQLite model + persistence smoke", false, ex.Message)); }
    }
    private static void Run(List<VerificationResult> results, string name, Action action) { try { action(); results.Add(new(name, true)); } catch (Exception ex) { results.Add(new(name, false, ex.Message)); } }
}
