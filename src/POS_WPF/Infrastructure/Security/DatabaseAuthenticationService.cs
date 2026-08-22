using Microsoft.EntityFrameworkCore;
using POS_WPF.Data;
using POS_WPF.Domain.Security;

namespace POS_WPF.Infrastructure.Security;

public sealed class DatabaseAuthenticationService(IDbContextFactory<AppDbContext> dbFactory, IPasswordHasher passwordHasher)
{
    public async Task<AuthenticationResult> AuthenticateAsync(string username, string password, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password)) return new(false, null, "Username and password are required.");
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        var user = await db.Users.SingleOrDefaultAsync(x => x.Username == username.Trim(), cancellationToken);
        if (user is null || !user.IsActive || !passwordHasher.Verify(password, user.PasswordHash)) return new(false, null, "Invalid credentials.");
        return new(true, user, null);
    }
}
