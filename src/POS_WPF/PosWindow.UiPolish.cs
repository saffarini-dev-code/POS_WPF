using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace POS_WPF;

public partial class PosWindow
{
    private bool _taxWatcherAttached;

    protected override void OnInitialized(EventArgs e)
    {
        base.OnInitialized(e);

        // Keep the approved cashier composition. The XAML owns the visual tree;
        // this only sets the minimum vertical space required by payment + keypad.
        if (Content is Grid root && root.RowDefinitions.Count >= 3)
        {
            var workspace = root.Children.OfType<Grid>().FirstOrDefault(x => Grid.GetRow(x) == 1);
            var rightPanel = workspace?.Children
                .OfType<Border>()
                .FirstOrDefault(x => Grid.GetColumn(x) == 1)
                ?.Child as Grid;

            if (rightPanel is not null && rightPanel.RowDefinitions.Count >= 4)
            {
                rightPanel.RowDefinitions[3].Height = new GridLength(210);
            }
        }

        AttachTaxVisibilityWatcher();
    }

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
