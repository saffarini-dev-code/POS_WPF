using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using Microsoft.EntityFrameworkCore;
using POS_WPF.Domain.Settings;

namespace POS_WPF;

public partial class PosWindow
{
    private bool _taxSettingsLoaded;
    private bool _taxEnabled;
    private decimal _taxRate;
    private bool _pricesIncludeTax;
    private bool _taxRefreshInProgress;

    protected override void OnInitialized(EventArgs e)
    {
        base.OnInitialized(e);

        Loaded += TaxSettings_Loaded;
        AddHandler(ButtonBase.ClickEvent, new RoutedEventHandler(Tax_ButtonClicked), true);
        AddHandler(TextBoxBase.TextChangedEvent, new TextChangedEventHandler(Tax_TextChanged), true);
        AddHandler(Mouse.PreviewMouseDownEvent, new MouseButtonEventHandler(Tax_PreviewMouseDown), true);
        AttachTaxVisibilityWatcher();
    }

    private async void TaxSettings_Loaded(object? sender, RoutedEventArgs e)
    {
        try
        {
            await using var db = await _dbFactory.CreateDbContextAsync();
            var settings = await db.TaxSettings.AsNoTracking().FirstOrDefaultAsync();
            _taxEnabled = settings?.IsEnabled == true;
            _taxRate = settings?.Rate ?? 0m;
            _pricesIncludeTax = settings?.PricesIncludeTax == true;
            _taxSettingsLoaded = true;
        }
        catch
        {
            _taxEnabled = false;
            _taxRate = 0m;
            _pricesIncludeTax = false;
            _taxSettingsLoaded = true;
        }

        await Dispatcher.InvokeAsync(RefreshTaxAndTotals, DispatcherPriority.Background);
    }

    private void Tax_ButtonClicked(object sender, RoutedEventArgs e)
    {
        if (!_taxSettingsLoaded) return;
        Dispatcher.BeginInvoke(RefreshTaxAndTotals, DispatcherPriority.Background);
    }

    private void Tax_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (!_taxSettingsLoaded || _taxRefreshInProgress) return;
        Dispatcher.BeginInvoke(RefreshTaxAndTotals, DispatcherPriority.Background);
    }

    private void Tax_PreviewMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (!_taxSettingsLoaded || e.ChangedButton != MouseButton.Left) return;
        if (IsDescendantOf(e.OriginalSource as DependencyObject, ChargeButton))
        {
            RefreshTaxAndTotals();
        }
    }

    private void RefreshTaxAndTotals()
    {
        if (!_taxSettingsLoaded || _taxRefreshInProgress) return;

        try
        {
            _taxRefreshInProgress = true;
            var invoiceDiscountRemaining = Math.Max(0m, _invoiceDiscount);
            var invoiceDiscountApplied = false;

            foreach (var item in _cart)
            {
                var lineBase = Math.Max(0m, item.Quantity * item.UnitPrice - item.ManualDiscount - item.PromotionDiscount);

                if (!invoiceDiscountApplied && invoiceDiscountRemaining > 0m)
                {
                    var applied = Math.Min(lineBase, invoiceDiscountRemaining);
                    lineBase -= applied;
                    invoiceDiscountRemaining -= applied;
                    invoiceDiscountApplied = invoiceDiscountRemaining <= 0m;
                }

                item.Tax = _taxEnabled ? CalculateTax(lineBase) : 0m;
            }

            RefreshTotal();
        }
        finally
        {
            _taxRefreshInProgress = false;
        }
    }

    private decimal CalculateTax(decimal taxableAmount)
    {
        if (!_taxEnabled || _taxRate <= 0m || taxableAmount <= 0m) return 0m;

        if (_pricesIncludeTax)
        {
            return Math.Round(taxableAmount - taxableAmount / (1m + _taxRate / 100m), 2, MidpointRounding.AwayFromZero);
        }

        return Math.Round(taxableAmount * (_taxRate / 100m), 2, MidpointRounding.AwayFromZero);
    }

    private static bool IsDescendantOf(DependencyObject? source, DependencyObject ancestor)
    {
        while (source is not null)
        {
            if (ReferenceEquals(source, ancestor)) return true;
            source = VisualTreeHelper.GetParent(source);
        }

        return false;
    }
}
