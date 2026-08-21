using Microsoft.EntityFrameworkCore;
using POS_WPF.Data;

namespace POS_WPF.Infrastructure.Security;

public sealed class PermissionService(IDbContextFactory<AppDbContext> dbFactory, SessionContext session)
{
    public async Task<bool> HasAsync(string permissionCode, CancellationToken cancellationToken = default)
    {
        if (session.CurrentUser is null) return false;
        if (string.Equals(session.CurrentRole, PermissionCatalog.SuperAdministrator, StringComparison.OrdinalIgnoreCase)) return true;
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        return await (from ur in db.UserRoles join rp in db.RolePermissions on ur.RoleId equals rp.RoleId where ur.UserId == session.CurrentUser.Id && rp.PermissionCode == permissionCode select rp.PermissionId).AnyAsync(cancellationToken);
    }

    public async Task DemandAsync(string permissionCode, CancellationToken cancellationToken = default)
    {
        if (!await HasAsync(permissionCode, cancellationToken)) throw new UnauthorizedAccessException($"Permission required: {permissionCode}");
    }
}
