using System.Windows;
using POS_WPF.Infrastructure.Security;

namespace POS_WPF;

public partial class ManagerAuthorizationWindow : Window
{
    private readonly ManagerAuthorizationService _authorization; private readonly string _permission;
    public ManagerAuthorizationWindow(ManagerAuthorizationService authorization, string permission) { InitializeComponent(); _authorization = authorization; _permission = permission; }
    private async void Authorize_Click(object sender, RoutedEventArgs e)
    { var allowed = await _authorization.AuthorizeAsync(UsernameBox.Text, PasswordBox.Password, _permission); if (allowed) { DialogResult = true; Close(); } else ErrorText.Text = "Authorization denied."; }
}
