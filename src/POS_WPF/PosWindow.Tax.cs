using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
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

        // Tax settings are loaded once when the POS is actually displayed.
        // Do not attach global TextChanged/Click handlers here: they fire for
        // every control during startup and can cause re-entrant total refreshes.
        Loaded += TaxSettings_Loaded;
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
            // POS must still open when tax configuration cannot be read.
            _taxEnabled = false;
            _taxRate = 0m;
            _pricesIncludeTax = false;
            _taxSettingsLoaded = true;
        }

        try
        {
            RefreshTaxAndTotals();
        }
        catch
        {
            // Never allow optional tax UI initialization to crash POS startup.
        }

        // Cash is the default payment method for a new POS sale.
        SelectPaymentMethod("Cash");
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
