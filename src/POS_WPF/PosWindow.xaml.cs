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
    public PosWindow(BarcodeLookupService barcode, SalePostingService posting, IDbContextFactory<AppDbContext> dbFactory, SessionContext session, IReceiptPrinter receiptPrinter) { InitializeComponent(); _barcode = barcode; _posting = posting; _dbFactory = dbFactory; _session = session; _receiptPrinter = receiptPrinter; CartGrid.ItemsSource = _cart; Loaded += (_, _) => { BarcodeBox.Focus(); RefreshTotal(); }; }
    private async void Barcode_KeyDown(object sender, KeyEventArgs e) { if (e.Key == Key.Enter) { e.Handled = true; await AddBarcodeAsync(); } }
    private async void Add_Click(object sender, RoutedEventArgs e) => await AddBarcodeAsync();
    private async Task AddBarcodeAsync()
    {
        var code = BarcodeBox.Text.Trim(); if (code.Length == 0) return;
        try
        {
            var match = await _barcode.FindAsync(code);
            if (match is null) { StatusText.Text = "Barcode not found."; return; }
            if (!match.Unit.CanSell) { StatusText.Text = "This unit is not sellable."; return; }
            var existing = _cart.FirstOrDefault(x => x.ProductId == match.Product.Id && x.UnitId == match.Unit.Id);
            if (existing is not null) existing.Quantity += 1; else _cart.Add(new CartItem(match.Product.Id, match.Unit.Id, match.Product.Name, match.Unit.Name, match.Unit.ConversionFactorToBase, match.Unit.SellingPrice));
            RefreshTotal(); StatusText.Text = $"Added {match.Product.Name} ({match.Unit.Abbreviation}).";
        }
        catch (Exception ex) { StatusText.Text = ex.InnerException?.Message ?? ex.Message; }
        finally { BarcodeBox.Clear(); BarcodeBox.Focus(); }
    }
    private async void Complete_Click(object sender, RoutedEventArgs e)
    {
        var total = _cart.Sum(x => x.LineTotal);
        if (_cart.Count == 0) { StatusText.Text = "Cart is empty."; return; }
        if (!decimal.TryParse(PaymentBox.Text, NumberStyles.Number, CultureInfo.CurrentCulture, out var payment) || payment < total) { StatusText.Text = $"Payment must be at least {total:N2}."; PaymentBox.Focus(); PaymentBox.SelectAll(); return; }
        try
        {
            await using var db = await _dbFactory.CreateDbContextAsync();
            var branch = await db.Branches.Where(x => x.IsActive).OrderBy(x => x.Code).FirstOrDefaultAsync();
            if (branch is null) { StatusText.Text = "No active branch is configured."; return; }
            var warehouse = await db.Warehouses.Where(x => x.IsActive && x.BranchId == branch.Id).OrderBy(x => x.Code).FirstOrDefaultAsync();
            var terminal = await db.Terminals.Where(x => x.IsActive && x.BranchId == branch.Id).OrderBy(x => x.Code).FirstOrDefaultAsync();
            var register = await db.CashRegisters.Where(x => x.IsOpen && x.BranchId == branch.Id).OrderBy(x => x.Code).FirstOrDefaultAsync();
            if (warehouse is null) { StatusText.Text = "No active warehouse is configured for the branch."; return; }
            if (terminal is null) { StatusText.Text = "No active POS terminal is configured for the branch."; return; }
            if (register is null) { StatusText.Text = "Open a cash register before selling."; return; }
            if (_session.CurrentUser is null) { StatusText.Text = "Session expired. Please sign in again."; return; }

            var number = $"S-{DateTime.UtcNow:yyyyMMddHHmmssfff}";
            var request = new SalePostingRequest(branch.Id, warehouse.Id, terminal.Id, register.Id, _session.CurrentUser.Id, number, null, _cart.Select(x => new SaleLineRequest(x.ProductId, x.UnitId, x.Quantity, x.UnitPrice, x.Discount, x.Tax)).ToList(), [new SalePaymentRequest("Cash", payment)]);
            var result = await _posting.PostAsync(request);
            _lastReceipt = BuildReceipt(number, result.Total, result.Change);
            _cart.Clear(); PaymentBox.Text = "0.00"; RefreshTotal();
            StatusText.Text = $"Sale completed successfully. Change: {result.Change:N2}";
            try { await _receiptPrinter.PrintAsync(new PrintDocumentRequest(string.Empty, _lastReceipt, "80mm")); StatusText.Text += " Receipt printed."; }
            catch (Exception printEx) { StatusText.Text += $" Sale is saved, but receipt printing failed: {printEx.Message}"; }
        }
        catch (Exception ex) { StatusText.Text = ex.InnerException?.Message ?? ex.Message; }
        finally { BarcodeBox.Focus(); }
    }
    private async void PrintLast_Click(object sender, RoutedEventArgs e) { if (string.IsNullOrWhiteSpace(_lastReceipt)) { StatusText.Text = "No receipt available for reprint."; return; } try { await _receiptPrinter.PrintAsync(new PrintDocumentRequest(string.Empty, _lastReceipt, "80mm")); StatusText.Text = "Receipt printed."; } catch (Exception ex) { StatusText.Text = ex.Message; } }
    private string BuildReceipt(string number, decimal total, decimal change) => $"RETAIL POS\n{number}\n------------------------------\nTOTAL: {total:N2}\nCHANGE: {change:N2}\nThank you\n";
    private void RefreshTotal() { var total = _cart.Sum(x => x.LineTotal); TotalText.Text = total.ToString("N2", CultureInfo.CurrentCulture); PaymentBox.Text = total.ToString("N2", CultureInfo.CurrentCulture); }
    private sealed class CartItem(Guid productId, Guid unitId, string productName, string unitName, decimal conversionFactor, decimal unitPrice)
    { public Guid ProductId { get; } = productId; public Guid UnitId { get; } = unitId; public string ProductName { get; } = productName; public string UnitName { get; } = unitName; public decimal ConversionFactor { get; } = conversionFactor; public decimal UnitPrice { get; } = unitPrice; public decimal Discount { get; set; } public decimal Tax { get; set; } public decimal Quantity { get; set; } = 1; public decimal LineTotal => Quantity * UnitPrice - Discount + Tax; }
}
