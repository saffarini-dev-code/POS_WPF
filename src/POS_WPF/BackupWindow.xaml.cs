using System.Windows;
using Microsoft.Extensions.Configuration;
using Microsoft.Win32;
using POS_WPF.Infrastructure.Backup;
using POS_WPF.Infrastructure.Security;

namespace POS_WPF;

public partial class BackupWindow : Window
{
    private readonly DatabaseBackupService _backup; private readonly PermissionService _permissions; private readonly IConfiguration _configuration;
    public BackupWindow(DatabaseBackupService backup, PermissionService permissions, IConfiguration configuration) { InitializeComponent(); _backup = backup; _permissions = permissions; _configuration = configuration; }
    private async void Backup_Click(object sender, RoutedEventArgs e)
    { try { await _permissions.DemandAsync("Settings.Store"); var dialog = new SaveFileDialog { Filter = "SQLite database (*.db)|*.db", FileName = $"POS-backup-{DateTime.Now:yyyyMMdd-HHmmss}.db" }; if (dialog.ShowDialog() != true) return; var source = _configuration["Database:ConnectionString"]?.Replace("Data Source=", string.Empty) ?? "pos-local.db"; await _backup.BackupSqliteAsync(source, dialog.FileName); StatusText.Text = $"Backup created: {dialog.FileName}"; } catch (Exception ex) { StatusText.Text = ex.Message; } }
    private async void Restore_Click(object sender, RoutedEventArgs e)
    { try { await _permissions.DemandAsync("Settings.Store"); var dialog = new OpenFileDialog { Filter = "SQLite database (*.db)|*.db" }; if (dialog.ShowDialog() != true) return; if (MessageBox.Show("Restoring replaces the current database. Continue?", "Confirm Restore", MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes) return; var source = _configuration["Database:ConnectionString"]?.Replace("Data Source=", string.Empty) ?? "pos-local.db"; await _backup.RestoreSqliteAsync(dialog.FileName, source); StatusText.Text = "Database restored. Restart the application before continuing."; } catch (Exception ex) { StatusText.Text = ex.Message; } }
}
