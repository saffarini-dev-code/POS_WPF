using System.Windows;
using POS_WPF.Infrastructure.Security;

namespace POS_WPF;

public partial class LoginWindow : Window
{
    private readonly DatabaseAuthenticationService _authentication;
    public LoginWindow(DatabaseAuthenticationService authentication)
    {
        InitializeComponent();
        _authentication = authentication;
        UsernameBox.Focus();
    }

    private async void LoginButton_Click(object sender, RoutedEventArgs e)
    {
        LoginButton.IsEnabled = false;
        ErrorText.Text = string.Empty;
        try
        {
            var result = await _authentication.AuthenticateAsync(UsernameBox.Text, PasswordBox.Password);
            if (!result.Succeeded)
            {
                ErrorText.Text = result.Error ?? "تعذر تسجيل الدخول. يرجى المحاولة مرة أخرى.";
                return;
            }
            var main = new MainWindow();
            main.Show();
            Close();
        }
        catch
        {
            ErrorText.Text = "تعذر تسجيل الدخول. يرجى المحاولة مرة أخرى.";
        }
        finally { LoginButton.IsEnabled = true; }
    }
}
