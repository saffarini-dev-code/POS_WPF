using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows;
using System.Windows.Input;
using Microsoft.EntityFrameworkCore;
using POS_WPF.Data;
using POS_WPF.Domain.POS;
using POS_WPF.Infrastructure.Printing;
using POS_WPF.Infrastructure.Security;

namespace POS_WPF;

public partial class PosWindow : Window
{
    private readonly BarcodeLookupService _barcode; private readonly SalePostingService _posting; private readonly IDbContextFactory<AppDbContext> _dbFactory; private readonly SessionContext _session; private readonly IReceiptPrinter _receiptPrinter; private readonly ObservableCollection<CartItem> _cart = []; private string? _lastReceipt;
    public PosWindow(BarcodeLookupService barcode, SalePostingService posting, IDbContextFactory<AppDbContext> dbFactory, SessionContext session, IReceiptPrinter receiptPrinter) { InitializeComponent(); _barcode = barcode; _posting = posting; _dbFactory = dbFactory; _session = session; _receiptPrinter = receiptPrinter; CartGrid.ItemsSource = _cart; Loaded += (_, _) => BarcodeBox.Focus(); }
    private async void Barcode_KeyDown(object sender, KeyEventArgs e) { if (e.Key == Key.Enter) { e.Handled = true; await AddBarcodeAsync(); } }
    private async void Add_Click(object sender, RoutedEventArgs e) => await AddBarcodeAsync();
    private async Task AddBarcodeAsync()
    {
        var code = BarcodeBox.Text.Trim(); if (code.Length == 0) return;
        try { var match = await _barcode.FindAsync(code); if (match is null) { StatusText.Text = "Barcode not found."; return; } if (!match.Unit.CanSell) { StatusText.Text = "This unit is not sellable."; return; } var existing = _cart.FirstOrDefault(x => x.ProductId == match.Product.Id && x.UnitId == match.Unit.Id); if (existing is not null) existing.Quantity += 1; else _cart.Add(new CartItem(match.Product.Id, match.Unit.Id, match.Product.Name, match.Unit.Name, match.Unit.ConversionFactorToBase, match.Unit.SellingPrice)); RefreshTotal(); StatusText.Text = $"Added {match.Product.Name} ({match.Unit.Abbreviation})."; } catch { StatusText.Text = "تعذر إضافة المنتج. يرجى المحاولة مرة أخرى."; } finally { BarcodeBox.Clear(); BarcodeBox.Focus(); }
    }
    private async void Complete_Click(object sender, RoutedEventArgs e)
    {
        if (_cart.Count == 0) { StatusText.Text = "Cart is empty."; return; } if (!decimal.TryParse(PaymentBox.Text, NumberStyles.Number, CultureInfo.CurrentCulture, out var payment) || payment <= 0) { StatusText.Text = "Enter a valid payment amount."; return; }
        try
        {
            await using var db = await _dbFactory.CreateDbContextAsync(); var branch = await db.Branches.Where(x => x.IsActive).OrderBy(x => x.Code).FirstAsync(); var warehouse = await db.Warehouses.Where(x => x.IsActive && x.BranchId == branch.Id).OrderBy(x => x.Code).FirstAsync(); var terminal = await db.Terminals.Where(x => x.IsActive && x.BranchId == branch.Id).OrderBy(x => x.Code).FirstAsync(); var register = await db.CashRegisters.Where(x => x.IsOpen && x.BranchId == branch.Id).OrderBy(x => x.Code).FirstOrDefaultAsync();
            if (register is null) { StatusText.Text = "Open a cash register before selling."; return; } if (_session.CurrentUser is null) { StatusText.Text = "Session expired."; return; }
            var number = $"S-{DateTime.UtcNow:yyyyMMddHHmmssfff}"; var request = new SalePostingRequest(branch.Id, warehouse.Id, terminal.Id, register.Id, _session.CurrentUser.Id, number, null, _cart.Select(x => new SaleLineRequest(x.ProductId, x.UnitId, x.Quantity, x.UnitPrice, x.Discount, x.Tax)).ToList(), [new SalePaymentRequest("Cash", payment)]); var result = await _posting.PostAsync(request);
            _lastReceipt = BuildReceipt(number, result.Total, result.Change); _cart.Clear(); PaymentBox.Clear(); RefreshTotal(); StatusText.Text = $"Sale completed. Change: {result.Change:N2}";
            await _receiptPrinter.PrintAsync(new PrintDocumentRequest(string.Empty, _lastReceipt, "80mm"));
        }
        catch (Exception ex) { StatusText.Text = ex is InvalidOperationException ? ex.Message : "تعذر حفظ العملية. يرجى المحاولة مرة أخرى."; }
        finally { BarcodeBox.Focus(); }
    }
    private async void PrintLast_Click(object sender, RoutedEventArgs e) { if (string.IsNullOrWhiteSpace(_lastReceipt)) { StatusText.Text = "No receipt available for reprint."; return; } await _receiptPrinter.PrintAsync(new PrintDocumentRequest(string.Empty, _lastReceipt, "80mm")); StatusText.Text = "Receipt printed."; }
    private string BuildReceipt(string number, decimal total, decimal change) => $"RETAIL POS\n{number}\n------------------------------\nTOTAL: {total:N2}\nCHANGE: {change:N2}\nThank you\n";
    private void RefreshTotal() => TotalText.Text = _cart.Sum(x => x.LineTotal).ToString("N2", CultureInfo.CurrentCulture);
    private sealed class CartItem(Guid productId, Guid unitId, string productName, string unitName, decimal conversionFactor, decimal unitPrice)
    { public Guid ProductId { get; } = productId; public Guid UnitId { get; } = unitId; public string ProductName { get; } = productName; public string UnitName { get; } = unitName; public decimal ConversionFactor { get; } = conversionFactor; public decimal UnitPrice { get; } = unitPrice; public decimal Discount { get; set; } public decimal Tax { get; set; } public decimal Quantity { get; set; } = 1; public decimal LineTotal => Quantity * UnitPrice - Discount + Tax; }
}
