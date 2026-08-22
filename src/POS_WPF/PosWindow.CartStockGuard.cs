using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

namespace POS_WPF;

public partial class PosWindow
{
    private static readonly bool _cartStockGuardRegistered = RegisterCartStockGuard();

    private static bool RegisterCartStockGuard()
    {
        EventManager.RegisterClassHandler(typeof(PosWindow), ButtonBase.ClickEvent, new RoutedEventHandler(OnCartQuantityIncreaseClick));
        return true;
    }

    private static void OnCartQuantityIncreaseClick(object sender, RoutedEventArgs e)
    {
        if (sender is not PosWindow window || e.OriginalSource is not Button button || button.DataContext is not CartItem item) return;
        if (!string.Equals(button.Content?.ToString(), "+", StringComparison.Ordinal)) return;
        e.Handled = true;
        _ = window.TryIncreaseCartQuantityAsync(item);
    }

    private async Task TryIncreaseCartQuantityAsync(CartItem item)
    {
        var requested = item.Quantity + 1m;
        if (!await HasSufficientStockAsync(item.ProductId, item.UnitId, requested))
        {
            Status("المخزون لا يكفي للإضافة.", false);
            return;
        }
        item.Quantity = requested;
        await RecalculateLineAsync(item);
    }
}
