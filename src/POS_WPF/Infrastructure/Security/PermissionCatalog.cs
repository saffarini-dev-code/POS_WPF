namespace POS_WPF.Infrastructure.Security;

public static class PermissionCatalog
{
    public static readonly string[] All =
    [
        "Sales.View", "Sales.Create", "Sales.Edit", "Sales.Cancel", "Sales.Return",
        "Products.View", "Products.Create", "Products.Edit", "Products.Delete",
        "Inventory.View", "Inventory.Adjust", "Inventory.Transfer",
        "Purchasing.View", "Purchasing.Create", "Purchasing.Return",
        "Customers.View", "Customers.Create", "Customers.Edit", "Customers.Delete",
        "Suppliers.View", "Suppliers.Create", "Suppliers.Edit", "Suppliers.Delete",
        "Reports.Sales", "Reports.Inventory", "Reports.Financial",
        "Payments.Create", "CashRegister.Open", "CashRegister.Close", "CashRegister.Adjust",
        "Settings.Store", "Settings.Invoice", "Settings.Tax", "Settings.Users", "Settings.Roles",
        "Audit.View", "Hardware.Configure", "Synchronization.Manage"
    ];

    public const string SuperAdministrator = "Super Administrator";
    public const string Manager = "Manager";
    public const string Cashier = "Cashier";
    public const string StoreKeeper = "Store Keeper";
    public const string Accountant = "Accountant";

    public static bool IsManagerAllowedToManageUser(string managerRole, string targetRole) =>
        !string.Equals(managerRole, Manager, StringComparison.OrdinalIgnoreCase) ||
        !string.Equals(targetRole, SuperAdministrator, StringComparison.OrdinalIgnoreCase);

    public static bool CanDeleteOrDisableSuperAdministrator(int currentSuperAdministratorCount, string targetRole) =>
        !string.Equals(targetRole, SuperAdministrator, StringComparison.OrdinalIgnoreCase) || currentSuperAdministratorCount > 1;
}
