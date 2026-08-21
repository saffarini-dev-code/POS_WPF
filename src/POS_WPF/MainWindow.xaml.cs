using System.Windows;
using Microsoft.Extensions.DependencyInjection;

namespace POS_WPF;

public partial class MainWindow : Window
{
    private readonly IServiceProvider _services;
    public MainWindow(IServiceProvider services) { InitializeComponent(); _services = services; }
    private void OpenPos_Click(object sender, RoutedEventArgs e) => _services.GetRequiredService<PosWindow>().Show();
    private void OpenProducts_Click(object sender, RoutedEventArgs e) => _services.GetRequiredService<ProductManagementWindow>().Show();
    private void OpenCashRegister_Click(object sender, RoutedEventArgs e) => _services.GetRequiredService<CashRegisterWindow>().Show();
    private void OpenReports_Click(object sender, RoutedEventArgs e) => _services.GetRequiredService<ReportsWindow>().Show();
    private void OpenSettings_Click(object sender, RoutedEventArgs e) => _services.GetRequiredService<SettingsWindow>().Show();
}
