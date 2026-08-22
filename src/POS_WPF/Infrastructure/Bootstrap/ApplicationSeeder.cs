using Microsoft.EntityFrameworkCore;
using POS_WPF.Data;
using POS_WPF.Domain.Security;
using POS_WPF.Domain.Settings;
using POS_WPF.Domain.Stores;
using POS_WPF.Infrastructure.Security;

namespace POS_WPF.Infrastructure.Bootstrap;

public sealed class ApplicationSeeder(AppDbContext db, IPasswordHasher passwordHasher)
{
    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        await EnsureRoleAsync(PermissionCatalog.SuperAdministrator, PermissionCatalog.All, cancellationToken); await EnsureRoleAsync(PermissionCatalog.Manager, PermissionCatalog.All.Where(x => x != "Settings.Users" && x != "Settings.Roles"), cancellationToken); await EnsureRoleAsync(PermissionCatalog.Cashier, ["Sales.View", "Sales.Create", "Sales.Return", "Products.View", "Customers.View", "Customers.Create", "Payments.Create", "CashRegister.Open", "CashRegister.Close"], cancellationToken); await EnsureRoleAsync(PermissionCatalog.StoreKeeper, ["Products.View", "Products.Create", "Products.Edit", "Inventory.View", "Inventory.Adjust", "Inventory.Transfer", "Purchasing.View", "Purchasing.Create", "Purchasing.Return", "Suppliers.View"], cancellationToken); await EnsureRoleAsync(PermissionCatalog.Accountant, ["Sales.View", "Sales.Return", "Purchasing.View", "Purchasing.Return", "Customers.View", "Suppliers.View", "Payments.Create", "Reports.Sales", "Reports.Inventory", "Reports.Financial"], cancellationToken);
        var superRole = await db.Roles.SingleAsync(x => x.Name == PermissionCatalog.SuperAdministrator, cancellationToken); var admin = await db.Users.SingleOrDefaultAsync(x => x.Username == "admin", cancellationToken); if (admin is null) { admin = new User("admin", "System Administrator", passwordHasher.Hash("ChangeMe123!"), mustChangePassword: true); db.Users.Add(admin); db.UserRoles.Add(new UserRole(admin.Id, superRole.Id)); }
        var branch = await db.Branches.OrderBy(x => x.Code).FirstOrDefaultAsync(cancellationToken);
        if (branch is null)
        {
            branch = new Branch("MAIN", "Main Branch"); db.Branches.Add(branch); await db.SaveChangesAsync(cancellationToken); db.Warehouses.Add(new Warehouse(branch.Id, "MAIN", "Main Warehouse")); db.Terminals.Add(new Terminal(branch.Id, "POS-01", "POS Terminal 01")); db.CashRegisters.Add(new CashRegister(branch.Id, "REG-01", "Cash Register 01"));
        }
        if (!await db.StoreSettings.AnyAsync(x => x.BranchId == branch.Id, cancellationToken)) { var settings = new StoreSettings("Retail POS", "JOD"); settings.AssignBranch(branch.Id); db.StoreSettings.Add(settings); }
        if (!await db.InvoiceSettings.AnyAsync(cancellationToken)) db.InvoiceSettings.Add(new InvoiceSettings()); if (!await db.TaxSettings.AnyAsync(cancellationToken))
        {
            var taxSettings = new TaxSettings();
            taxSettings.Configure(false, 0m, false, true);
            db.TaxSettings.Add(taxSettings);
        }
        await db.SaveChangesAsync(cancellationToken);
    }
    private async Task EnsureRoleAsync(string name, IEnumerable<string> permissions, CancellationToken cancellationToken) { var role = await db.Roles.SingleOrDefaultAsync(x => x.Name == name, cancellationToken); if (role is null) { role = new Role(name); db.Roles.Add(role); await db.SaveChangesAsync(cancellationToken); } var existing = await db.RolePermissions.Where(x => x.RoleId == role.Id).Select(x => x.PermissionCode).ToListAsync(cancellationToken); foreach (var permission in permissions.Where(x => !existing.Contains(x))) db.RolePermissions.Add(new RolePermission(role.Id, permission)); await db.SaveChangesAsync(cancellationToken); }
}
