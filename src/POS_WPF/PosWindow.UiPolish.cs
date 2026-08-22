using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace POS_WPF;

public partial class PosWindow
{
    private bool _taxWatcherAttached;

    private void AttachTaxVisibilityWatcher()
    {
        if (_taxWatcherAttached || ReceiptTaxText is null) return;
        _taxWatcherAttached = true;

        var descriptor = DependencyPropertyDescriptor.FromProperty(TextBlock.TextProperty, typeof(TextBlock));
        descriptor?.AddValueChanged(ReceiptTaxText, (_, _) => UpdateTaxVisibility());
        UpdateTaxVisibility();
    }

    private void UpdateTaxVisibility()
    {
        if (ReceiptTaxText is null) return;

        var hasTax = decimal.TryParse(
            ReceiptTaxText.Text,
            System.Globalization.NumberStyles.Number,
            System.Globalization.CultureInfo.CurrentCulture,
            out var tax) && Math.Abs(tax) > 0.000001m;

        TaxLabel.Visibility = hasTax ? Visibility.Visible : Visibility.Collapsed;
        TaxText.Visibility = hasTax ? Visibility.Visible : Visibility.Collapsed;
    }
}
