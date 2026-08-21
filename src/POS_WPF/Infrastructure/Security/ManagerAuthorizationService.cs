using Microsoft.EntityFrameworkCore;
using POS_WPF.Data;

namespace POS_WPF.Infrastructure.Security;

public sealed class ManagerAuthorizationService(IDbContextFactory<AppDbContext> dbFactory, IPasswordHasher hasher)
{
    public async Task<bool> AuthorizeAsync(string username, string password, string permissionCode, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password)) return false;
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        var user = await db.Users.SingleOrDefaultAsync(x => x.Username == username.Trim() && x.IsActive, cancellationToken); if (user is null || !hasher.Verify(password, user.PasswordHash)) return false;
        return await (from ur in db.UserRoles join role in db.Roles on ur.RoleId equals role.Id join permission in db.RolePermissions on role.Id equals permission.RoleId where ur.UserId == user.Id && permission.PermissionCode == permissionCode select permission.PermissionId).AnyAsync(cancellationToken);
    }
}
