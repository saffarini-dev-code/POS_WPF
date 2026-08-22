using System.Collections.ObjectModel;
using System.Globalization;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using Microsoft.EntityFrameworkCore;
using POS_WPF.Data;
using POS_WPF.Domain.POS;
using POS_WPF.Domain.Promotions;
using POS_WPF.Domain.Sales;
using POS_WPF.Infrastructure.Printing;
using POS_WPF.Infrastructure.Security;

namespace POS_WPF;

public partial class PosWindow : Window
{
    private readonly BarcodeLookupService _barcode;
    private readonly SalePostingService _posting;
    private readonly IDbContextFactory<AppDbContext> _dbFactory;
    private readonly SessionContext _session;
    private readonly IReceiptPrinter _receiptPrinter;
    private readonly ObservableCollection<CartItem> _cart = [];
    private string? _lastReceipt;
    private decimal _invoiceDiscount;
    private string _paymentMethod = "Card";
    private readonly DispatcherTimer _clock = new() { Interval = TimeSpan.FromSeconds(1) };

    public PosWindow(BarcodeLookupService barcode, SalePostingService posting, IDbContextFactory<AppDbContext> dbFactory, SessionContext session, IReceiptPrinter receiptPrinter)
    {
        InitializeComponent();
        _barcode = barcode;
        _posting = posting;
        _dbFactory = dbFactory;
        _session = session;
        _receiptPrinter = receiptPrinter;
        CartGrid.ItemsSource = _cart;
        ReceiptItemsPanel.ItemsSource = _cart;
        Loaded += async (_, _) =>
        {
            _clock.Tick += Clock_Tick;
            _clock.Start();
            await LoadPopularProductsAsync();
            await LoadHeldAsync();
            SelectPaymentMethod("Card");
            BarcodeBox.Focus();
            RefreshTotal();
        };
        Closed += (_, _) => _clock.Stop();
    }

    private void Clock_Tick(object? sender, EventArgs e) => ClockText.Text = DateTime.Now.ToString("ddd, MMM dd hh:mm:ss tt", CultureInfo.InvariantCulture);

    private async Task LoadPopularProductsAsync()
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var sold = await db.Set<SaleLine>().Where(l => db.Sales.Any(s => s.Id == EF.Property<Guid>(l, "SaleId") && s.Status == SaleStatus.Completed)).GroupBy(l => l.ProductId).Select(g => new { ProductId = g.Key, Sold = g.Sum(x => x.Quantity * x.ConversionFactor) }).OrderByDescending(x => x.Sold).Take(20).ToListAsync();
        if (sold.Count == 0)
        {
            var fallback = await db.Products.AsNoTracking().Where(x => x.IsActive).OrderBy(x => x.Name).Take(20).Include(x => x.Units).ToListAsync();
            PopularProductsPanel.ItemsSource = fallback.Select(p => { var unit = p.Units.SingleOrDefault(u => u.Id == p.BaseUnitId) ?? p.Units.SingleOrDefault(u => u.IsBaseUnit); return new PopularProduct(p.Id, unit?.Id ?? Guid.Empty, p.Sku, p.Name, unit?.SellingPrice ?? 0m, 0m, unit?.Abbreviation ?? unit?.Abbreviation ?? unit?.Abbreviation ?? unit?.Abbreviation ?? unit?.Name ?? "PCS"); }).ToList();
            return;
        }
        var ids = sold.Select(x => x.ProductId).ToList();
        var products = await db.Products.AsNoTracking().Where(x => ids.Contains(x.Id) && x.IsActive).Include(x => x.Units).ToListAsync();
        PopularProductsPanel.ItemsSource = sold.Join(products, x => x.ProductId, x => x.Id, (x, p) => { var unit = p.Units.SingleOrDefault(u => u.Id == p.BaseUnitId) ?? p.Units.SingleOrDefault(u => u.IsBaseUnit); return new PopularProduct(p.Id, unit?.Id ?? Guid.Empty, p.Sku, p.Name, unit?.SellingPrice ?? 0m, x.Sold, unit?.Abbreviation ?? unit?.Abbreviation ?? unit?.Abbreviation ?? unit?.Abbreviation ?? unit?.Name ?? "PCS"); }).ToList();
    }

    private async void PopularProduct_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not FrameworkElement { DataContext: PopularProduct item }) return;
        try
        {
            if (item.UnitId == Guid.Empty) throw new InvalidOperationException("The product has no base selling unit.");
            var existing = _cart.FirstOrDefault(x => x.ProductId == item.ProductId && x.UnitId == item.UnitId);
            if (existing is null) { existing = new CartItem(item.ProductId, item.UnitId, item.Name, item.UnitName, 1m, item.Price); _cart.Add(existing); }
            else existing.Quantity += 1;
            await ApplyPromotionAsync(existing);
            RefreshTotal();
            Status($"Added {item.Name}.", true);
        }
        catch (Exception ex) { Status(ex.Message, false); }
    }

    private async void Barcode_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter) return;
        e.Handled = true;
        await AddBarcodeAsync();
    }

    private async Task AddBarcodeAsync()
    {
        var code = BarcodeBox.Text.Trim();
        if (code.Length == 0) return;
        try
        {
            var match = await _barcode.FindAsync(code);
            if (match is null) { Status("Barcode not found.", false); return; }
            if (!match.Unit.CanSell) { Status("This unit is not sellable.", false); return; }
            var existing = _cart.FirstOrDefault(x => x.ProductId == match.Product.Id && x.UnitId == match.Unit.Id);
            if (existing is null) { existing = new CartItem(match.Product.Id, match.Unit.Id, match.Product.Name, match.Unit.Name, match.Unit.ConversionFactorToBase, match.Unit.SellingPrice); _cart.Add(existing); }
            else existing.Quantity += 1;
            await ApplyPromotionAsync(existing);
            RefreshTotal();
            Status($"Added {match.Product.Name} ({match.Unit.Abbreviation}).", true);
        }
        catch (Exception ex) { Status(ex.InnerException?.Message ?? ex.Message, false); }
        finally { BarcodeBox.Clear(); BarcodeBox.Focus(); }
    }

    private async Task ApplyPromotionAsync(CartItem item)
    {
        item.PromotionDiscount = 0;
        item.PromotionId = null;
        item.AppliedPromotionQuantity = 0;
        item.PromotionName = null;
        await using var db = await _dbFactory.CreateDbContextAsync();
        var now = DateTime.UtcNow;
        var promotions = await db.Promotions.AsNoTracking().Where(x => x.IsActive && x.ProductId == item.ProductId && (!x.UnitId.HasValue || x.UnitId == item.UnitId)).OrderByDescending(x => x.StartsAtUtc).ToListAsync();
        var promotion = promotions.FirstOrDefault(x => x.IsCurrentlyActive(now));
        if (promotion is null) return;
        var eligibleQuantity = item.Quantity;
        if (promotion.MaxQuantity.HasValue) eligibleQuantity = Math.Min(eligibleQuantity, Math.Max(0, promotion.MaxQuantity.Value - promotion.ConsumedQuantity));
        if (eligibleQuantity <= 0) return;
        var lineBase = item.Quantity * item.UnitPrice;
        switch (promotion.Type)
        {
            case PromotionType.Percentage:
                item.PromotionDiscount = Math.Round(lineBase * (promotion.Value / 100m) * (eligibleQuantity / item.Quantity), 2, MidpointRounding.AwayFromZero);
                item.AppliedPromotionQuantity = eligibleQuantity;
                break;
            case PromotionType.FixedAmount:
                item.PromotionDiscount = Math.Min(lineBase * (eligibleQuantity / item.Quantity), promotion.Value);
                item.AppliedPromotionQuantity = eligibleQuantity;
                break;
            case PromotionType.QuantityDiscount:
                if (promotion.MinimumQuantity <= 0) return;
                var groups = Math.Floor(eligibleQuantity / promotion.MinimumQuantity);
                item.PromotionDiscount = Math.Max(0, groups * (promotion.MinimumQuantity * item.UnitPrice - promotion.Value));
                item.AppliedPromotionQuantity = groups * promotion.MinimumQuantity;
                break;
            case PromotionType.BuyXGetY:
                if (promotion.MinimumQuantity <= 0 || promotion.RewardQuantity <= 0) return;
                var buyGroups = Math.Floor(eligibleQuantity / promotion.MinimumQuantity);
                item.PromotionDiscount = Math.Min(lineBase, buyGroups * promotion.RewardQuantity * item.UnitPrice);
                item.AppliedPromotionQuantity = buyGroups * promotion.MinimumQuantity;
                break;
        }
        item.PromotionName = promotion.Name;
        item.PromotionId = promotion.Id;
    }

    private async void Cart_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
    {
        if (e.Row.Item is not CartItem item) return;
        await Dispatcher.InvokeAsync(async () =>
        {
            if (item.Quantity <= 0) { _cart.Remove(item); RefreshTotal(); return; }
            if (item.ManualDiscount < 0) item.ManualDiscount = 0;
            await ApplyPromotionAsync(item);
            RefreshTotal();
        });
    }

    private void IncreaseQuantity_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { Tag: CartItem item }) { item.Quantity += 1; _ = RecalculateLineAsync(item); }
    }

    private void DecreaseQuantity_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not FrameworkElement { Tag: CartItem item }) return;
        item.Quantity -= 1;
        if (item.Quantity <= 0) _cart.Remove(item);
        else _ = RecalculateLineAsync(item);
        RefreshTotal();
    }

    private async Task RecalculateLineAsync(CartItem item) { await ApplyPromotionAsync(item); RefreshTotal(); }

    private void PaymentMethod_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { Tag: string method }) SelectPaymentMethod(method);
    }

    private void SelectPaymentMethod(string method)
    {
        _paymentMethod = method;
        PaymentMethodBox.SelectedIndex = method switch { "Card" => 1, "Mobile" => 2, _ => 0 };
        CardButton.Background = method == "Card" ? System.Windows.Media.Brushes.LightGreen : System.Windows.Media.Brushes.White;
        CashButton.Background = method == "Cash" ? System.Windows.Media.Brushes.LightGreen : System.Windows.Media.Brushes.White;
        MobileButton.Background = method == "Mobile" ? System.Windows.Media.Brushes.LightGreen : System.Windows.Media.Brushes.White;
        ReceiptPaymentText.Text = method.ToUpperInvariant();
    }

    private void ApplyInvoiceDiscount_Click(object sender, RoutedEventArgs e)
    {
        if (!decimal.TryParse(InvoiceDiscountBox.Text, NumberStyles.Number, CultureInfo.CurrentCulture, out var discount) || discount < 0) { Status("Enter a valid invoice discount.", false); return; }
        var max = Math.Max(0, _cart.Sum(x => x.Quantity * x.UnitPrice - x.ManualDiscount - x.PromotionDiscount + x.Tax));
        _invoiceDiscount = Math.Min(discount, max);
        InvoiceDiscountBox.Text = _invoiceDiscount.ToString("N2", CultureInfo.CurrentCulture);
        RefreshTaxAndTotals();
        Status("Invoice discount applied.", true);
    }

    private void ClearCart_Click(object sender, RoutedEventArgs e)
    {
        _cart.Clear();
        _invoiceDiscount = 0;
        InvoiceDiscountBox.Text = "0.00";
        OrderNumberText.Text = $"TXN-{Random.Shared.Next(100000, 999999)}";
        RefreshTotal();
        Status("Cart cleared.", true);
    }

    private async Task LoadHeldAsync()
    {
        if (_session.CurrentUser is null) { HeldOrdersBox.ItemsSource = null; return; }
        await using var db = await _dbFactory.CreateDbContextAsync();
        HeldOrdersBox.ItemsSource = await db.HeldSales.AsNoTracking().Where(x => x.CashierId == _session.CurrentUser.Id && !x.IsReleased).OrderByDescending(x => x.CreatedAtUtc).Select(x => new HeldOrderOption(x.Id, $"{x.Reference} · {x.CreatedAtUtc.ToLocalTime():HH:mm}")).ToListAsync();
    }

    private async void Hold_Click(object sender, RoutedEventArgs e)
    {
        if (_cart.Count == 0) { Status("An empty cart cannot be held.", false); return; }
        if (_session.CurrentUser is null) { Status("Session expired.", false); return; }
        try
        {
            var reference = string.IsNullOrWhiteSpace(HoldLabelBox.Text) ? $"HOLD-{DateTime.Now:HHmmss}" : HoldLabelBox.Text.Trim();
            var payload = JsonSerializer.Serialize(new HeldCartPayload(_cart.Select(x => new CartSnapshot(x.ProductId, x.UnitId, x.ProductName, x.UnitName, x.ConversionFactor, x.UnitPrice, x.Quantity, x.ManualDiscount, x.PromotionDiscount, x.Tax, x.PromotionId, x.AppliedPromotionQuantity, x.PromotionName)).ToList(), _invoiceDiscount));
            await using var db = await _dbFactory.CreateDbContextAsync();
            db.HeldSales.Add(new HeldSale(reference, _session.CurrentUser.Id, payload));
            await db.SaveChangesAsync();
            _cart.Clear();
            _invoiceDiscount = 0;
            InvoiceDiscountBox.Text = "0.00";
            HoldLabelBox.Clear();
            OrderNumberText.Text = $"TXN-{Random.Shared.Next(100000, 999999)}";
            RefreshTotal();
            await LoadHeldAsync();
            Status($"Order held successfully: {reference}.", true);
        }
        catch (Exception ex) { Status(ex.InnerException?.Message ?? ex.Message, false); }
    }

    private async void Recall_Click(object sender, RoutedEventArgs e)
    {
        if (HeldOrdersBox.Items.Count == 0) { Status("There are no held orders.", false); return; }
        var selected = HeldOrdersBox.Items.Cast<HeldOrderOption>().FirstOrDefault();
        if (selected is null) { Status("No held order is available.", false); return; }
        await RecallHeldAsync(selected.Id);
    }

    private async Task RecallHeldAsync(Guid id)
    {
        if (_cart.Count > 0) { Status("Clear or hold the current cart before recalling another order.", false); return; }
        try
        {
            await using var db = await _dbFactory.CreateDbContextAsync();
            var held = await db.HeldSales.SingleAsync(x => x.Id == id && !x.IsReleased);
            var payload = JsonSerializer.Deserialize<HeldCartPayload>(held.Payload) ?? throw new InvalidOperationException("Held order data is invalid.");
            foreach (var x in payload.Items)
            {
                _cart.Add(new CartItem(x.ProductId, x.UnitId, x.ProductName, x.UnitName, x.ConversionFactor, x.UnitPrice)
                {
                    Quantity = x.Quantity,
                    ManualDiscount = x.ManualDiscount,
                    PromotionDiscount = x.PromotionDiscount,
                    Tax = x.Tax,
                    PromotionId = x.PromotionId,
                    AppliedPromotionQuantity = x.AppliedPromotionQuantity,
                    PromotionName = x.PromotionName
                });
            }
            _invoiceDiscount = payload.InvoiceDiscount;
            InvoiceDiscountBox.Text = _invoiceDiscount.ToString("N2", CultureInfo.CurrentCulture);
            held.IsReleased = true;
            await db.SaveChangesAsync();
            OrderNumberText.Text = held.Reference;
            await LoadHeldAsync();
            RefreshTotal();
            Status($"Held order recalled: {held.Reference}.", true);
        }
        catch (Exception ex) { Status(ex.InnerException?.Message ?? ex.Message, false); }
    }

    private async void Complete_Click(object sender, RoutedEventArgs e)
    {
        var total = GetInvoiceTotal();
        if (_cart.Count == 0) { Status("Cart is empty.", false); return; }
        if (!decimal.TryParse(PaymentBox.Text, NumberStyles.Number, CultureInfo.CurrentCulture, out var payment) || payment < total)
        {
            Status($"Payment must be at least {total:N2}.", false);
            PaymentBox.Focus();
            PaymentBox.SelectAll();
            return;
        }
        try
        {
            await using var db = await _dbFactory.CreateDbContextAsync();
            var branch = await db.Branches.Where(x => x.IsActive).OrderBy(x => x.Code).FirstOrDefaultAsync();
            if (branch is null) { Status("No active branch is configured.", false); return; }
            var warehouse = await db.Warehouses.Where(x => x.IsActive && x.BranchId == branch.Id).OrderBy(x => x.Code).FirstOrDefaultAsync();
            var terminal = await db.Terminals.Where(x => x.IsActive && x.BranchId == branch.Id).OrderBy(x => x.Code).FirstOrDefaultAsync();
            var register = await db.CashRegisters.Where(x => x.IsOpen && x.BranchId == branch.Id).OrderBy(x => x.Code).FirstOrDefaultAsync();
            if (warehouse is null) { Status("No active warehouse is configured for the branch.", false); return; }
            if (terminal is null) { Status("No active POS terminal is configured for the branch.", false); return; }
            if (register is null) { Status("Open a cash register before selling.", false); return; }
            if (_session.CurrentUser is null) { Status("Session expired. Please sign in again.", false); return; }

            var number = $"S-{DateTime.UtcNow:yyyyMMddHHmmssfff}";
            var requests = _cart.Select(x => new SaleLineRequest(x.ProductId, x.UnitId, x.Quantity, x.UnitPrice, x.Discount, 0m)).ToList();
            if (_invoiceDiscount > 0 && requests.Count > 0) { var first = requests[0]; requests[0] = first with { Discount = first.Discount + _invoiceDiscount }; }
            var request = new SalePostingRequest(branch.Id, warehouse.Id, terminal.Id, register.Id, _session.CurrentUser.Id, number, null, requests, [new SalePaymentRequest(_paymentMethod, payment)]);
            var result = await _posting.PostAsync(request);

            foreach (var item in _cart.Where(x => x.PromotionId.HasValue && x.AppliedPromotionQuantity > 0))
            {
                var promo = await db.Promotions.SingleOrDefaultAsync(x => x.Id == item.PromotionId!.Value);
                if (promo is not null)
                {
                    promo.ConsumedQuantity += item.AppliedPromotionQuantity;
                    if (promo.MaxQuantity.HasValue && promo.ConsumedQuantity >= promo.MaxQuantity.Value) promo.IsActive = false;
                }
            }
            await db.SaveChangesAsync();

            _lastReceipt = BuildReceipt(number, result.Total, result.Change);
            ReceiptOrderText.Text = number;
            ReceiptTenderedText.Text = payment.ToString("N2", CultureInfo.CurrentCulture);
            ReceiptChangeText.Text = result.Change.ToString("N2", CultureInfo.CurrentCulture);
            _cart.Clear();
            _invoiceDiscount = 0;
            InvoiceDiscountBox.Text = "0.00";
            PaymentBox.Text = "0.00";
            OrderNumberText.Text = $"TXN-{Random.Shared.Next(100000, 999999)}";
            RefreshTotal();
            await LoadPopularProductsAsync();
            await LoadHeldAsync();
            Status($"Sale completed successfully. Change: {result.Change:N2}", true);
            try
            {
                await _receiptPrinter.PrintAsync(new PrintDocumentRequest(string.Empty, _lastReceipt, "80mm"));
                Status($"Sale completed successfully. Change: {result.Change:N2} · Receipt printed.", true);
            }
            catch (Exception printEx) { Status($"Sale is saved, but receipt printing failed: {printEx.Message}", false); }
        }
        catch (Exception ex) { Status(ex.InnerException?.Message ?? ex.Message, false); }
        finally { BarcodeBox.Focus(); }
    }

    private string BuildReceipt(string number, decimal total, decimal change)
    {
        var subtotal = _cart.Sum(x => x.Quantity * x.UnitPrice);
        var discount = _cart.Sum(x => x.Discount) + _invoiceDiscount;
        var tax = _invoiceTax;
        var taxLine = tax > 0m ? $"TAX: {tax:N2}{Environment.NewLine}" : string.Empty;
        return $"RETAIL POS{Environment.NewLine}{number}{Environment.NewLine}------------------------------{Environment.NewLine}SUBTOTAL: {subtotal:N2}{Environment.NewLine}DISCOUNT: {discount:N2}{Environment.NewLine}{taxLine}TOTAL: {total:N2}{Environment.NewLine}CHANGE: {change:N2}{Environment.NewLine}Thank you{Environment.NewLine}";
    }

    private void RefreshTotal()
    {
        foreach (var item in _cart)
        {
            var maxManual = Math.Max(0, item.Quantity * item.UnitPrice - item.PromotionDiscount);
            if (item.ManualDiscount > maxManual) item.ManualDiscount = maxManual;
        }
        var subtotal = _cart.Sum(x => x.Quantity * x.UnitPrice);
        var lineDiscount = _cart.Sum(x => x.Discount);
        var tax = _invoiceTax;
        var maxInvoice = Math.Max(0, subtotal - lineDiscount + tax);
        if (_invoiceDiscount > maxInvoice) _invoiceDiscount = maxInvoice;
        var total = Math.Max(0, subtotal - lineDiscount - _invoiceDiscount + tax);
        SubtotalText.Text = subtotal.ToString("N2", CultureInfo.CurrentCulture);
        DiscountText.Text = (lineDiscount + _invoiceDiscount).ToString("N2", CultureInfo.CurrentCulture);
        TotalText.Text = total.ToString("N2", CultureInfo.CurrentCulture);
        PaymentBox.Text = total.ToString("N2", CultureInfo.CurrentCulture);
        ReceiptSubtotalText.Text = subtotal.ToString("N2", CultureInfo.CurrentCulture);
        ReceiptDiscountText.Text = (lineDiscount + _invoiceDiscount).ToString("N2", CultureInfo.CurrentCulture);
        ReceiptTaxText.Text = tax.ToString("N2", CultureInfo.CurrentCulture);
        ReceiptTotalText.Text = total.ToString("N2", CultureInfo.CurrentCulture);
        ReceiptTenderedText.Text = total.ToString("N2", CultureInfo.CurrentCulture);
        ReceiptChangeText.Text = "0.00";
        ReceiptItemsCountText.Text = $"{_cart.Sum(x => x.Quantity):N0} ITEMS · {_cart.Count:N0} LINES";
        FooterItemsText.Text = _cart.Sum(x => x.Quantity).ToString("N0", CultureInfo.CurrentCulture);
        FooterLinesText.Text = _cart.Count.ToString("N0", CultureInfo.CurrentCulture);
        FooterSubtotalText.Text = subtotal.ToString("N2", CultureInfo.CurrentCulture);
        ReceiptDateText.Text = DateTime.Now.ToString("MM/dd/yy hh:mm tt", CultureInfo.InvariantCulture);
        CartGrid.Items.Refresh();
        ReceiptItemsPanel.Items.Refresh();
    }

    private void Status(string message, bool success)
    {
        StatusText.Foreground = success ? System.Windows.Media.Brushes.DarkGreen : System.Windows.Media.Brushes.DarkRed;
        StatusText.Text = success ? "✓  " + message : "⚠  " + message;
    }

    private sealed record PopularProduct(Guid ProductId, Guid UnitId, string Sku, string Name, decimal Price, decimal Sold, string UnitName)
    {
        public string SkuText => string.IsNullOrWhiteSpace(Sku) ? "SKU" : Sku;
        public string PriceText => Price.ToString("N2", CultureInfo.CurrentCulture);
        public string SoldText => Sold > 0 ? $"×{Sold:N0}" : string.Empty;
    }

    private sealed record HeldOrderOption(Guid Id, string Display);
    private sealed record HeldCartPayload(List<CartSnapshot> Items, decimal InvoiceDiscount);
    private sealed record CartSnapshot(Guid ProductId, Guid UnitId, string ProductName, string UnitName, decimal ConversionFactor, decimal UnitPrice, decimal Quantity, decimal ManualDiscount, decimal PromotionDiscount, decimal Tax, Guid? PromotionId, decimal AppliedPromotionQuantity, string? PromotionName);

    private sealed class CartItem(Guid productId, Guid unitId, string productName, string unitName, decimal conversionFactor, decimal unitPrice)
    {
        public Guid ProductId { get; } = productId;
        public Guid UnitId { get; } = unitId;
        public string ProductName { get; } = productName;
        public string UnitName { get; } = unitName;
        public decimal ConversionFactor { get; } = conversionFactor;
        public decimal UnitPrice { get; } = unitPrice;
        public decimal ManualDiscount { get; set; }
        public decimal PromotionDiscount { get; set; }
        public decimal Tax { get; set; }
        public decimal Quantity { get; set; } = 1;
        public Guid? PromotionId { get; set; }
        public decimal AppliedPromotionQuantity { get; set; }
        public string? PromotionName { get; set; }
        public decimal Discount => ManualDiscount + PromotionDiscount;
        public decimal LineTotal => Math.Max(0, Quantity * UnitPrice - Discount);
    }
}