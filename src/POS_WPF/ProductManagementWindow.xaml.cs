using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using Microsoft.EntityFrameworkCore;
using POS_WPF.Data;
using POS_WPF.Domain.Audit;
using POS_WPF.Domain.Products;
using POS_WPF.Infrastructure.Security;

namespace POS_WPF;

public partial class ProductManagementWindow : Window
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory; private readonly UnitConversionService _conversion; private readonly PermissionService _permissions; private readonly SessionContext _session; private Guid? _selectedId; private readonly ObservableCollection<ProductUnit> _units = []; private List<ProductListItem> _products = [];
    public ProductManagementWindow(IDbContextFactory<AppDbContext> dbFactory, UnitConversionService conversion, PermissionService permissions, SessionContext session)
    { InitializeComponent(); _dbFactory = dbFactory; _conversion = conversion; _permissions = permissions; _session = session; UnitsGrid.ItemsSource = _units; Loaded += async (_, _) => await LoadAsync(); }
    private async Task LoadAsync()
    { await using var db = await _dbFactory.CreateDbContextAsync(); _products = await db.Products.AsNoTracking().OrderBy(x => x.Name).Select(x => new ProductListItem(x.Id, x.Sku, x.Name, x.NameArabic)).ToListAsync(); ProductsGrid.ItemsSource = _products; }
    private async void Refresh_Click(object sender, RoutedEventArgs e) => await LoadAsync();
    private void Search_Changed(object sender, TextChangedEventArgs e) { var term = SearchBox.Text.Trim(); ProductsGrid.ItemsSource = string.IsNullOrEmpty(term) ? _products : _products.Where(x => x.Name.Contains(term, StringComparison.OrdinalIgnoreCase) || x.Sku.Contains(term, StringComparison.OrdinalIgnoreCase)).ToList(); }
    private async void Product_Selected(object sender, SelectionChangedEventArgs e)
    { if (ProductsGrid.SelectedItem is not ProductListItem item) return; _selectedId = item.Id; SkuBox.Text = item.Sku; NameBox.Text = item.Name; ArabicNameBox.Text = item.NameArabic ?? string.Empty; await using var db = await _dbFactory.CreateDbContextAsync(); var units = await db.ProductUnits.AsNoTracking().Where(x => x.ProductId == item.Id).OrderByDescending(x => x.IsBaseUnit).ThenBy(x => x.Name).ToListAsync(); _units.Clear(); foreach (var unit in units) _units.Add(unit); StatusText.Text = string.Empty; }
    private void AddUnit_Click(object sender, RoutedEventArgs e) { _units.Add(new ProductUnit { Name = "PCS", Abbreviation = "PCS", ConversionFactorToBase = 1m, IsBaseUnit = _units.Count == 0, CanSell = true, CanPurchase = true, IsActive = true }); }
    private async void Save_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            await _permissions.DemandAsync(_selectedId.HasValue ? "Products.Edit" : "Products.Create");
            if (string.IsNullOrWhiteSpace(SkuBox.Text) || string.IsNullOrWhiteSpace(NameBox.Text)) throw new InvalidOperationException("SKU and product name are required.");
            if (_units.Count == 0) throw new InvalidOperationException("A product must have at least one unit.");
            var product = new Product { Id = _selectedId ?? Guid.NewGuid(), Sku = SkuBox.Text.Trim(), Name = NameBox.Text.Trim(), NameArabic = ArabicNameBox.Text.Trim(), IsActive = true, Units = _units };
            if (Guid.TryParse(CategoryBox.Text, out var categoryId)) product.CategoryId = categoryId;
            _conversion.ValidateProductUnits(product); var baseUnit = _units.Single(x => x.IsBaseUnit); if (baseUnit.Id == Guid.Empty) baseUnit.Id = Guid.NewGuid(); product.BaseUnitId = baseUnit.Id; foreach (var unit in _units) { if (unit.Id == Guid.Empty) unit.Id = Guid.NewGuid(); unit.ProductId = product.Id; }
            await using var db = await _dbFactory.CreateDbContextAsync();
            if (_selectedId is null) { db.Products.Add(product); db.ProductUnits.AddRange(_units); }
            else { var existing = await db.Products.SingleAsync(x => x.Id == product.Id); existing.Sku = product.Sku; existing.Name = product.Name; existing.NameArabic = product.NameArabic; existing.CategoryId = product.CategoryId; existing.BaseUnitId = product.BaseUnitId; db.ProductUnits.UpdateRange(_units); }
            db.AuditEntries.Add(new AuditEntry(_session.CurrentUser?.Id, _selectedId is null ? "Product.Created" : "Product.Updated", nameof(Product), product.Id, null, product.Sku, null, null, "Product Management"));
            await db.SaveChangesAsync(); await LoadAsync(); StatusText.Foreground = System.Windows.Media.Brushes.DarkGreen; StatusText.Text = "Saved.";
        }
        catch (Exception ex) { StatusText.Foreground = System.Windows.Media.Brushes.DarkRed; StatusText.Text = ex.Message; }
    }
    private sealed record ProductListItem(Guid Id, string Sku, string Name, string? NameArabic);
}
