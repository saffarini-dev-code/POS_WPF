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
        window.ShowPaymentConfirmation();
    }

    private void ShowPaymentConfirmation()
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

        var dialog = new PaymentConfirmationDialog(total, payment, _paymentMethod, this);
        var previousOpacity = Opacity;
        Opacity = 0.72;
        try
        {
            var confirmed = dialog.ShowDialog() == true;
            if (confirmed)
            {
                Complete_Click(this, new RoutedEventArgs());
            }
        }
        finally
        {
            if (IsVisible) Opacity = previousOpacity;
        }
    }
}
