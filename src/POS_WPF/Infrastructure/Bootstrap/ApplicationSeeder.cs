using Microsoft.EntityFrameworkCore;
using POS_WPF.Data;
using POS_WPF.Domain.Security;
using POS_WPF.Domain.Stores;
using POS_WPF.Infrastructure.Security;

namespace POS_WPF.Infrastructure.Bootstrap;

public sealed class ApplicationSeeder(AppDbContext db, IPasswordHasher passwordHasher)
{
    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        var superRole = await db.Roles.SingleOrDefaultAsync(x => x.Name == PermissionCatalog.SuperAdministrator, cancellationToken);
        if (superRole is null)
        {
            superRole = new Role(PermissionCatalog.SuperAdministrator);
            db.Roles.Add(superRole);
            await db.SaveChangesAsync(cancellationToken);
            foreach (var code in PermissionCatalog.All) db.RolePermissions.Add(new RolePermission(superRole.Id, code));
        }

        var managerRole = await db.Roles.SingleOrDefaultAsync(x => x.Name == PermissionCatalog.Manager, cancellationToken);
        if (managerRole is null)
        {
            managerRole = new Role(PermissionCatalog.Manager);
            db.Roles.Add(managerRole);
            await db.SaveChangesAsync(cancellationToken);
            foreach (var code in PermissionCatalog.All.Where(x => x != "Settings.Users" && x != "Settings.Roles")) db.RolePermissions.Add(new RolePermission(managerRole.Id, code));
        }

        var admin = await db.Users.SingleOrDefaultAsync(x => x.Username == "admin", cancellationToken);
        if (admin is null)
        {
            admin = new User("admin", "System Administrator", passwordHasher.Hash("ChangeMe123!"), mustChangePassword: true);
            db.Users.Add(admin);
            db.UserRoles.Add(new UserRole(admin.Id, superRole.Id));
        }

        if (!await db.Branches.AnyAsync(cancellationToken))
        {
            var branch = new Branch("MAIN", "Main Branch"); db.Branches.Add(branch);
            await db.SaveChangesAsync(cancellationToken);
            db.Warehouses.Add(new Warehouse(branch.Id, "MAIN", "Main Warehouse"));
            db.Terminals.Add(new Terminal(branch.Id, "POS-01", "POS Terminal 01"));
            db.CashRegisters.Add(new CashRegister(branch.Id, "REG-01", "Cash Register 01"));
        }
        await db.SaveChangesAsync(cancellationToken);
    }
}
