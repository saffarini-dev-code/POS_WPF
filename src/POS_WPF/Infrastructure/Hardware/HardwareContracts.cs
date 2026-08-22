namespace POS_WPF.Infrastructure.Hardware;

public interface IBarcodeScanner
{
    event EventHandler<string>? BarcodeScanned;
    Task StartAsync(CancellationToken cancellationToken = default);
    Task StopAsync(CancellationToken cancellationToken = default);
}

public interface ICashDrawer
{
    Task OpenAsync(CancellationToken cancellationToken = default);
}

public interface ICustomerDisplay
{
    Task ShowAsync(string message, CancellationToken cancellationToken = default);
}
