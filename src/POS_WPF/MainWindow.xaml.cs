using System.Windows;
using Microsoft.Extensions.DependencyInjection;

namespace POS_WPF;

public partial class MainWindow : Window
{
    private readonly IServiceProvider _services;
    public MainWindow(IServiceProvider services) { InitializeComponent(); _services = services; }
    private void OpenPos_Click(object sender, RoutedEventArgs e) => _services.GetRequiredService<PosWindow>().Show();
    private void OpenStore_Click(object sender, RoutedEventArgs e) => _services.GetRequiredService<StoreManagementWindow>().Show();
    private void OpenCategories_Click(object sender, RoutedEventArgs e) => _services.GetRequiredService<CategoryManagementWindow>().Show();
    private void OpenProducts_Click(object sender, RoutedEventArgs e) => _services.GetRequiredService<ProductManagementWindow>().Show();
    private void OpenInventory_Click(object sender, RoutedEventArgs e) => _services.GetRequiredService<InventoryManagementWindow>().Show();
    private void OpenOpeningStock_Click(object sender, RoutedEventArgs e) => _services.GetRequiredService<OpeningStockWindow>().Show();
    private void OpenCashRegister_Click(object sender, RoutedEventArgs e) => _services.GetRequiredService<CashRegisterWindow>().Show();
    private void OpenReports_Click(object sender, RoutedEventArgs e) => _services.GetRequiredService<ReportsWindow>().Show();
    private void OpenSettings_Click(object sender, RoutedEventArgs e) => _services.GetRequiredService<SettingsWindow>().Show();
    private void OpenCustomers_Click(object sender, RoutedEventArgs e) => _services.GetRequiredService<AccountsWindow>().Show();
    private void OpenSuppliers_Click(object sender, RoutedEventArgs e) => _services.GetRequiredService<AccountsWindow>().Show();
}
