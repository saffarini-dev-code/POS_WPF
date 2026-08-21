using System.Windows;
using System.Windows.Controls;
using Microsoft.EntityFrameworkCore;
using POS_WPF.Data;
using POS_WPF.Domain.Inventory;
using POS_WPF.Infrastructure.Security;

namespace POS_WPF;

public partial class InventoryManagementWindow : Window
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory; private readonly InventoryBalanceService _balances; private readonly InventoryOperationsService _operations; private readonly SessionContext _session; private List<StockRow> _rows = []; private StockRow? _selected;
    public InventoryManagementWindow(IDbContextFactory<AppDbContext> dbFactory, InventoryBalanceService balances, InventoryOperationsService operations, SessionContext session) { InitializeComponent(); _dbFactory = dbFactory; _balances = balances; _operations = operations; _session = session; Loaded += async (_, _) => { await LoadWarehousesAsync(); await LoadAsync(); }; }
    private async Task LoadWarehousesAsync() { await using var db = await _dbFactory.CreateDbContextAsync(); WarehouseBox.ItemsSource = await db.Warehouses.AsNoTracking().Where(x => x.IsActive).OrderBy(x => x.Code).Select(x => new WarehouseOption(x.Id, $"{x.Code} — {x.Name}", x.BranchId)).ToListAsync(); if (WarehouseBox.Items.Count > 0) WarehouseBox.SelectedIndex = 0; }
    private async void Warehouse_Changed(object sender, SelectionChangedEventArgs e) { await LoadAsync(); }
    private async Task LoadAsync()
    {
        if (WarehouseBox.SelectedValue is not Guid warehouseId) return; await using var db = await _dbFactory.CreateDbContextAsync(); var products = await db.Products.AsNoTracking().Where(x => x.IsActive).Include(x => x.Units).Include(x => x.Category).OrderBy(x => x.Name).ToListAsync(); var rows = new List<StockRow>(); foreach (var p in products) { var baseUnit = p.Units.SingleOrDefault(x => x.Id == p.BaseUnitId) ?? p.Units.SingleOrDefault(x => x.IsBaseUnit); var stock = await _balances.GetBaseBalanceAsync(p.Id, warehouseId); rows.Add(new StockRow(p.Id, baseUnit?.Id ?? Guid.Empty, p.Sku, p.Name, p.Category?.Name ?? "—", baseUnit?.Abbreviation ?? "PCS", stock)); } _rows = rows; StockGrid.ItemsSource = _rows;
    }
    private void Stock_Selected(object sender, SelectionChangedEventArgs e) { _selected = StockGrid.SelectedItem as StockRow; SelectedProductText.Text = _selected is null ? "None" : $"{_selected.Sku} — {_selected.Name} ({_selected.Stock:N3} {_selected.Unit})"; }
    private async void Adjust_Click(object sender, RoutedEventArgs e)
    {
        try { if (_selected is null) throw new InvalidOperationException("Select a product first."); if (WarehouseBox.SelectedItem is not WarehouseOption warehouse) throw new InvalidOperationException("Select a warehouse."); if (_session.CurrentUser is null) throw new InvalidOperationException("Session expired."); if (!decimal.TryParse(QuantityBox.Text, out var quantity) || quantity <= 0) throw new InvalidOperationException("Enter a valid quantity."); var reason = string.IsNullOrWhiteSpace(ReasonBox.Text) ? "Manual inventory adjustment" : ReasonBox.Text.Trim(); await _operations.AdjustAsync(new InventoryAdjustmentRequest(warehouse.BranchId, warehouse.Id, _session.CurrentUser.Id, _selected.ProductId, _selected.UnitId, quantity, DirectionBox.SelectedIndex == 0, reason, $"ADJ-{DateTime.UtcNow:yyyyMMddHHmmssfff}")); StatusText.Text = "Inventory adjustment posted."; await LoadAsync(); }
        catch (Exception ex) { StatusText.Foreground = System.Windows.Media.Brushes.OrangeRed; StatusText.Text = ex.InnerException?.Message ?? ex.Message; }
    }
    private sealed record WarehouseOption(Guid Id, string Display, Guid BranchId); private sealed record StockRow(Guid ProductId, Guid UnitId, string Sku, string Name, string Category, string Unit, decimal Stock) { public string Status => Stock <= 0 ? "Out of Stock" : Stock <= 5 ? "Low Stock" : "In Stock"; }
}