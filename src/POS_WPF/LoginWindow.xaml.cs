using System.Windows;
using Microsoft.EntityFrameworkCore;
using POS_WPF.Infrastructure.Security;

namespace POS_WPF;

public partial class LoginWindow : Window
{
    private readonly DatabaseAuthenticationService _authentication;
    private readonly IDbContextFactory<Data.AppDbContext> _dbFactory;
    private readonly IPasswordHasher _hasher;
    private readonly SessionContext _session;
    public LoginWindow(DatabaseAuthenticationService authentication, IDbContextFactory<Data.AppDbContext> dbFactory, IPasswordHasher hasher, SessionContext session)
    { InitializeComponent(); _authentication = authentication; _dbFactory = dbFactory; _hasher = hasher; _session = session; UsernameBox.Focus(); }
    private async void LoginButton_Click(object sender, RoutedEventArgs e)
    {
        LoginButton.IsEnabled = false; ErrorText.Text = string.Empty;
        try
        {
            var result = await _authentication.AuthenticateAsync(UsernameBox.Text, PasswordBox.Password);
            if (!result.Succeeded) { ErrorText.Text = result.Error ?? "تعذر تسجيل الدخول. يرجى المحاولة مرة أخرى."; return; }
            if (result.User!.MustChangePassword)
            {
                var passwordWindow = new PasswordChangeWindow(_dbFactory, _hasher, result.User);
                if (passwordWindow.ShowDialog() != true) return;
            }
            await using var db = await _dbFactory.CreateDbContextAsync();
            var role = await (from ur in db.UserRoles join r in db.Roles on ur.RoleId equals r.Id where ur.UserId == result.User.Id select r.Name).FirstOrDefaultAsync() ?? PermissionCatalog.Cashier;
            _session.SignIn(result.User, role);
            new MainWindow().Show(); Close();
        }
        catch { ErrorText.Text = "تعذر تسجيل الدخول. يرجى المحاولة مرة أخرى."; }
        finally { LoginButton.IsEnabled = true; }
    }
}
