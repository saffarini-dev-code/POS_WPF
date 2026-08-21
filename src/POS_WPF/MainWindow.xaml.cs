using System.Windows;
using Microsoft.Extensions.DependencyInjection;

namespace POS_WPF;

public partial class MainWindow : Window
{
    private readonly IServiceProvider _services;
    public MainWindow(IServiceProvider services) { InitializeComponent(); _services = services; }
    private void OpenPos_Click(object sender, RoutedEventArgs e) => _services.GetRequiredService<PosWindow>().Show();
}
