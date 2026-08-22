using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace POS_WPF;

public partial class PosWindow
{
    private static readonly bool _dialogPolishRegistered = RegisterDialogPolishHandler();

    private static bool RegisterDialogPolishHandler()
    {
        EventManager.RegisterClassHandler(typeof(Window), FrameworkElement.LoadedEvent, new RoutedEventHandler(PolishCompactDialog));
        return true;
    }

    private static void PolishCompactDialog(object sender, RoutedEventArgs e)
    {
        if (sender is not Window dialog) return;
        if (dialog is PaymentConfirmationDialog) return;
        if (!string.Equals(dialog.Title, "Authorize Close POS", StringComparison.Ordinal) && !string.Equals(dialog.Title, "تأكيد حذف المنتج", StringComparison.Ordinal)) return;
        dialog.Dispatcher.BeginInvoke(new Action(() => ApplyCompactDialogWindow(dialog)), System.Windows.Threading.DispatcherPriority.Loaded);
    }

    private static void ApplyCompactDialogWindow(Window dialog)
    {
        if (!dialog.IsVisible) return;
        dialog.WindowState = WindowState.Normal;
        dialog.SizeToContent = SizeToContent.Manual;
        dialog.Width = 384;
        dialog.Height = dialog.Title == "Authorize Close POS" ? 300 : 270;
        dialog.MinWidth = dialog.MaxWidth = dialog.Width;
        dialog.MinHeight = dialog.MaxHeight = dialog.Height;
        dialog.WindowStyle = WindowStyle.None;
        dialog.ResizeMode = ResizeMode.NoResize;
        dialog.AllowsTransparency = true;
        dialog.Background = Brushes.Transparent;
        dialog.ShowInTaskbar = false;
        dialog.WindowStartupLocation = WindowStartupLocation.CenterOwner;
        dialog.Topmost = false;

        if (dialog.Content is Border) return;
        if (dialog.Content is not FrameworkElement content) return;
        content.Margin = new Thickness(0);
        dialog.Content = new Border
        {
            Width = dialog.Width,
            Height = dialog.Height,
            Background = Brushes.White,
            CornerRadius = new CornerRadius(10),
            BorderBrush = new SolidColorBrush(Color.FromRgb(226, 232, 240)),
            BorderThickness = new Thickness(1),
            Padding = new Thickness(24),
            Child = content
        };
    }
}
