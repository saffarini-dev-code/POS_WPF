using System.Windows;
using Microsoft.EntityFrameworkCore;
using POS_WPF.Data;
using POS_WPF.Domain.Security;
using POS_WPF.Infrastructure.Security;

namespace POS_WPF;

public partial class PasswordChangeWindow : Window
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;
    private readonly IPasswordHasher _hasher;
    private readonly User _user;
    public PasswordChangeWindow(IDbContextFactory<AppDbContext> dbFactory, IPasswordHasher hasher, User user)
    {
        InitializeComponent(); _dbFactory = dbFactory; _hasher = hasher; _user = user;
    }
    private async void Save_Click(object sender, RoutedEventArgs e)
    {
        if (NewPassword.Password.Length < 10 || NewPassword.Password != ConfirmPassword.Password)
        { ErrorText.Text = "Password must be at least 10 characters and both values must match."; return; }
        await using var db = await _dbFactory.CreateDbContextAsync();
        var user = await db.Users.SingleAsync(x => x.Id == _user.Id);
        user.ChangePasswordHash(_hasher.Hash(NewPassword.Password));
        await db.SaveChangesAsync();
        DialogResult = true;
        Close();
    }
}
