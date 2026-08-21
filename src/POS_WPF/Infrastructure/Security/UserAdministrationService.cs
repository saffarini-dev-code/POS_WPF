using Microsoft.EntityFrameworkCore;
using POS_WPF.Data;

namespace POS_WPF.Infrastructure.Security;

public sealed class UserAdministrationService(IDbContextFactory<AppDbContext> dbFactory)
{
    public async Task<IReadOnlyList<(Guid Id, string Username, string DisplayName)>> GetVisibleUsersAsync(string currentRole, CancellationToken cancellationToken = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        var query = from user in db.Users
                    join userRole in db.UserRoles on user.Id equals userRole.UserId
                    join role in db.Roles on userRole.RoleId equals role.Id
                    where user.IsActive
                    select new { user.Id, user.Username, user.DisplayName, Role = role.Name };
        if (string.Equals(currentRole, PermissionCatalog.Manager, StringComparison.OrdinalIgnoreCase))
            query = query.Where(x => x.Role != PermissionCatalog.SuperAdministrator);
        return await query.AsNoTracking().Select(x => ValueTuple.Create(x.Id, x.Username, x.DisplayName)).ToListAsync(cancellationToken);
    }

    public async Task<bool> CanDisableAsync(Guid targetUserId, CancellationToken cancellationToken = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        var isSuperAdmin = await (from ur in db.UserRoles join r in db.Roles on ur.RoleId equals r.Id where ur.UserId == targetUserId select r.Name).AnyAsync(x => x == PermissionCatalog.SuperAdministrator, cancellationToken);
        if (!isSuperAdmin) return true;
        var count = await (from ur in db.UserRoles join r in db.Roles on ur.RoleId equals r.Id where r.Name == PermissionCatalog.SuperAdministrator select ur.UserId).Distinct().CountAsync(cancellationToken);
        return count > 1;
    }
}
