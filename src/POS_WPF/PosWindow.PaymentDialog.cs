using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

namespace POS_WPF;

public partial class PosWindow
{
    static PosWindow()
    {
        EventManager.RegisterClassHandler(
            typeof(PosWindow),
            ButtonBase.ClickEvent,
            new RoutedEventHandler(InterceptChargeClick));
    }

    private static void InterceptChargeClick(object sender, RoutedEventArgs e)
    {
        if (sender is not PosWindow window || e.OriginalSource is not Button button ||
            !string.Equals(button.Name, "ChargeButton", StringComparison.Ordinal)) return;

        e.Handled = true;
        _ = window.ShowPaymentConfirmationAsync();
    }

    private async Task ShowPaymentConfirmationAsync()
    {
        var total = GetInvoiceTotal();
        if (_cart.Count == 0)
        {
            Status("Cart is empty.", false);
            return;
        }

        if (!decimal.TryParse(PaymentBox.Text, System.Globalization.NumberStyles.Number,
                System.Globalization.CultureInfo.CurrentCulture, out var payment) || payment < total)
        {
            Status($"Payment must be at least {total:N2}.", false);
            PaymentBox.Focus();
            PaymentBox.SelectAll();
            return;
        }

        var dialog = new PaymentConfirmationDialog(total, payment, _paymentMethod, this, CompleteSaleFromPaymentDialogAsync);
        var previousOpacity = Opacity;
        Opacity = 0.72;
        try
        {
            dialog.ShowDialog();
        }
        finally
        {
            if (IsVisible) Opacity = previousOpacity;
            BarcodeBox.Focus();
        }
    }

    private async Task<bool> CompleteSaleFromPaymentDialogAsync()
    {
        if (_cart.Count == 0) return false;

        var initialItemCount = _cart.Count;
        Complete_Click(this, new RoutedEventArgs());

        var deadline = DateTime.UtcNow.AddSeconds(15);
        while (DateTime.UtcNow < deadline)
        {
            await Task.Delay(50);
            if (_cart.Count == 0)
                return true;
        }

        return initialItemCount > 0 && _cart.Count == 0;
    }
}
