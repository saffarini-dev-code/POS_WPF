using System.Collections.Specialized;
using System.Windows;
using System.Windows.Controls.Primitives;
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
    private decimal _invoiceTax;

    private decimal GetInvoiceTotal()
    {
        var subtotal = _cart.Sum(x => x.Quantity * x.UnitPrice);
        var discount = _cart.Sum(x => x.Discount);
        return Math.Max(0m, subtotal - discount - _invoiceDiscount + _invoiceTax);
    }

    protected override void OnInitialized(EventArgs e)
    {
        base.OnInitialized(e);
        Loaded += TaxSettings_Loaded;
        AttachTaxVisibilityWatcher();
        _cart.CollectionChanged += Cart_CollectionChangedForTax;
        InitializeCartUi();
        Loaded += (_, _) => Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() => SelectPaymentMethod("Cash")));
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

        AttachTaxInteractionWatchers();
        RefreshTaxAndTotals();
    }

    private void AttachTaxInteractionWatchers()
    {
        if (CartGrid is not null)
            CartGrid.AddHandler(ButtonBase.ClickEvent, new RoutedEventHandler((_, _) => Dispatcher.BeginInvoke(DispatcherPriority.DataBind, new Action(RefreshTaxAndTotals))), true);

        if (InvoiceDiscountBox is not null)
            InvoiceDiscountBox.LostKeyboardFocus += (_, _) => Dispatcher.BeginInvoke(DispatcherPriority.DataBind, new Action(RefreshTaxAndTotals));
    }

    private void RefreshTaxAndTotals()
    {
        if (!_taxSettingsLoaded || _taxRefreshInProgress) return;

        try
        {
            _taxRefreshInProgress = true;
            foreach (var item in _cart) item.Tax = 0m;

            var subtotal = _cart.Sum(x => x.Quantity * x.UnitPrice);
            var lineDiscount = _cart.Sum(x => x.Discount);
            var taxableAmount = Math.Max(0m, subtotal - lineDiscount - Math.Max(0m, _invoiceDiscount));
            _invoiceTax = _taxEnabled ? CalculateTax(taxableAmount) : 0m;

            RefreshTotal();

            var discount = lineDiscount + _invoiceDiscount;
            var total = Math.Max(0m, subtotal - discount + _invoiceTax);
            SubtotalText.Text = subtotal.ToString("N2", System.Globalization.CultureInfo.CurrentCulture);
            DiscountText.Text = discount.ToString("N2", System.Globalization.CultureInfo.CurrentCulture);
            ReceiptSubtotalText.Text = SubtotalText.Text;
            ReceiptDiscountText.Text = DiscountText.Text;
            ReceiptTaxText.Text = _showTaxOnInvoice ? _invoiceTax.ToString("N2", System.Globalization.CultureInfo.CurrentCulture) : "0.00";
            ReceiptTotalText.Text = total.ToString("N2", System.Globalization.CultureInfo.CurrentCulture);
            TotalText.Text = total.ToString("N2", System.Globalization.CultureInfo.CurrentCulture);
            PaymentBox.Text = total.ToString("N2", System.Globalization.CultureInfo.CurrentCulture);
            CartGrid.Items.Refresh();
            ReceiptItemsPanel.Items.Refresh();
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
            return Math.Round(taxableAmount - taxableAmount / (1m + _taxRate / 100m), 2, MidpointRounding.AwayFromZero);
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
