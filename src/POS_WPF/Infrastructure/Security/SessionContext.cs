using POS_WPF.Domain.Security;

namespace POS_WPF.Infrastructure.Security;

public sealed class SessionContext
{
    public User? CurrentUser { get; private set; }
    public string CurrentRole { get; private set; } = string.Empty;
    public void SignIn(User user, string role) { CurrentUser = user; CurrentRole = role; }
    public void SignOut() { CurrentUser = null; CurrentRole = string.Empty; }
}
