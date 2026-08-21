using POS_WPF.Domain.Security;

namespace POS_WPF.Infrastructure.Security;

public sealed record AuthenticationResult(bool Succeeded, User? User, string? Error);

public interface IAuthenticationService
{
    Task<AuthenticationResult> AuthenticateAsync(string username, string password, CancellationToken cancellationToken = default);
}

public sealed class AuthenticationService(
    IPasswordHasher passwordHasher,
    Func<CancellationToken, Task<User?>> userResolver) : IAuthenticationService
{
    public async Task<AuthenticationResult> AuthenticateAsync(string username, string password, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            return new(false, null, "Username and password are required.");

        var user = await userResolver(cancellationToken);
        if (user is null || !user.IsActive || !string.Equals(user.Username, username.Trim(), StringComparison.OrdinalIgnoreCase))
            return new(false, null, "Invalid credentials.");

        return passwordHasher.Verify(password, user.PasswordHash)
            ? new(true, user, null)
            : new(false, null, "Invalid credentials.");
    }
}
