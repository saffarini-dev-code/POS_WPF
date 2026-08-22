using System.Collections.Specialized;
using System.Windows;
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
    private bool _showTaxOnInvoice;

    protected override void OnInitialized(EventArgs e)
    {
        base.OnInitialized(e);

        Loaded += TaxSettings_Loaded;
        AttachTaxVisibilityWatcher();

        // Recalculate tax whenever products are added/removed from the cart.
        // Quantity/discount changes are explicitly refreshed by the cart controls.
        _cart.CollectionChanged += Cart_CollectionChangedForTax;

        // Load the programmatic cart template after InitializeComponent has created
        // all named controls, while keeping the existing XAML layout intact.
        InitializeCartUi();

        Loaded += (_, _) => Dispatcher.BeginInvoke(
            DispatcherPriority.Background,
            new Action(() => SelectPaymentMethod("Cash")));
    }

    private void Cart_CollectionChangedForTax(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (!IsLoaded) return;
        Dispatcher.BeginInvoke(DispatcherPriority.DataBind, new Action(RefreshTaxAndTotals));
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
            _showTaxOnInvoice = settings?.ShowTaxOnInvoice == true && _taxEnabled;
            _taxSettingsLoaded = true;
        }
        catch
        {
            _taxEnabled = false;
            _taxRate = 0m;
            _pricesIncludeTax = false;
            _showTaxOnInvoice = false;
            _taxSettingsLoaded = true;
        }

        try
        {
            RefreshTaxAndTotals();
        }
        catch
        {
            // Tax is optional UI behavior; never let its initialization crash POS.
        }
    }

    private void RefreshTaxAndTotals()
    {
        if (!_taxSettingsLoaded || _taxRefreshInProgress) return;

        try
        {
            _taxRefreshInProgress = true;
            var invoiceDiscountRemaining = Math.Max(0m, _invoiceDiscount);

            foreach (var item in _cart)
            {
                var lineBase = Math.Max(0m, item.Quantity * item.UnitPrice - item.ManualDiscount - item.PromotionDiscount);

                if (invoiceDiscountRemaining > 0m)
                {
                    var applied = Math.Min(lineBase, invoiceDiscountRemaining);
                    lineBase -= applied;
                    invoiceDiscountRemaining -= applied;
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
