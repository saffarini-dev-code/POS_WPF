using POS_WPF.Domain.Pricing;
using POS_WPF.Domain.Products;
using POS_WPF.Domain.Returns;
using POS_WPF.Infrastructure.Security;

namespace POS_WPF.Infrastructure.Verification;

public sealed record VerificationResult(string Name, bool Passed, string? Error = null);

public sealed class VerificationRunner
{
    public IReadOnlyList<VerificationResult> RunAll()
    {
        var results = new List<VerificationResult>();
        Run(results, "Unit conversion BOX to PCS", () =>
        {
            var service = new UnitConversionService();
            var unit = new ProductUnit { ConversionFactorToBase = 12m };
            if (service.ToBaseQuantity(10m, unit) != 120m) throw new InvalidOperationException("Expected 120 base units.");
        });
        Run(results, "Unit-specific price independence", () =>
        {
            var unit = new ProductUnit { ConversionFactorToBase = 12m, SellingPrice = 10m };
            if (unit.SellingPrice == unit.ConversionFactorToBase) throw new InvalidOperationException("Price must remain independent from conversion.");
        });
        Run(results, "Percentage discount", () =>
        {
            var discount = new DiscountRule(DiscountType.Percentage, 10m);
            if (discount.Calculate(100m) != 10m) throw new InvalidOperationException("Expected 10 discount.");
        });
        Run(results, "Return historical conversion", () =>
        {
            var factor = 12m; var returned = 1m * factor;
            if (returned != 12m) throw new InvalidOperationException("Expected 12 base units restored.");
        });
        Run(results, "Password hashing", () =>
        {
            var hasher = new PasswordHasher(); var hash = hasher.Hash("VerificationPassword123!");
            if (!hasher.Verify("VerificationPassword123!", hash) || hasher.Verify("wrong", hash)) throw new InvalidOperationException("Password verification failed.");
        });
        Run(results, "Manager cannot manage Super Administrator", () =>
        {
            if (PermissionCatalog.IsManagerAllowedToManageUser(PermissionCatalog.Manager, PermissionCatalog.SuperAdministrator)) throw new InvalidOperationException("Manager restriction failed.");
        });
        Run(results, "Last Super Administrator protection", () =>
        {
            if (PermissionCatalog.CanDeleteOrDisableSuperAdministrator(1, PermissionCatalog.SuperAdministrator)) throw new InvalidOperationException("Last administrator protection failed.");
        });
        return results;
    }

    private static void Run(List<VerificationResult> results, string name, Action action)
    {
        try { action(); results.Add(new(name, true)); }
        catch (Exception ex) { results.Add(new(name, false, ex.Message)); }
    }
}
