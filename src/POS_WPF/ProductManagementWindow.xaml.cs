using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using Microsoft.EntityFrameworkCore;
using POS_WPF.Data;
using POS_WPF.Domain.Products;

namespace POS_WPF;

public partial class ProductManagementWindow : Window
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;
    private readonly UnitConversionService _conversion;
    private Guid? _selectedId;
    private readonly ObservableCollection<ProductUnit> _units = [];
    private List<ProductListItem> _products = [];
    public ProductManagementWindow(IDbContextFactory<AppDbContext> dbFactory, UnitConversionService conversion)
    { InitializeComponent(); _dbFactory = dbFactory; _conversion = conversion; UnitsGrid.ItemsSource = _units; Loaded += async (_, _) => await LoadAsync(); }

    private async Task LoadAsync()
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        _products = await db.Products.AsNoTracking().OrderBy(x => x.Name).Select(x => new ProductListItem(x.Id, x.Sku, x.Name, x.NameArabic)).ToListAsync();
        ProductsGrid.ItemsSource = _products;
    }
    private async void Refresh_Click(object sender, RoutedEventArgs e) => await LoadAsync();
    private async void Search_Changed(object sender, TextChangedEventArgs e)
    {
        var term = SearchBox.Text.Trim();
        ProductsGrid.ItemsSource = string.IsNullOrEmpty(term) ? _products : _products.Where(x => x.Name.Contains(term, StringComparison.OrdinalIgnoreCase) || x.Sku.Contains(term, StringComparison.OrdinalIgnoreCase)).ToList();
    }
    private async void Product_Selected(object sender, SelectionChangedEventArgs e)
    {
        if (ProductsGrid.SelectedItem is not ProductListItem item) return;
        _selectedId = item.Id; SkuBox.Text = item.Sku; NameBox.Text = item.Name; ArabicNameBox.Text = item.NameArabic ?? string.Empty;
        await using var db = await _dbFactory.CreateDbContextAsync();
        var units = await db.ProductUnits.AsNoTracking().Where(x => x.ProductId == item.Id).OrderByDescending(x => x.IsBaseUnit).ThenBy(x => x.Name).ToListAsync();
        _units.Clear(); foreach (var unit in units) _units.Add(unit);
        StatusText.Text = string.Empty;
    }
    private void AddUnit_Click(object sender, RoutedEventArgs e)
    { _units.Add(new ProductUnit { Name = "PCS", Abbreviation = "PCS", ConversionFactorToBase = 1m, IsBaseUnit = _units.Count == 0, CanSell = true, CanPurchase = true, IsActive = true }); }
    private async void Save_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(SkuBox.Text) || string.IsNullOrWhiteSpace(NameBox.Text)) throw new InvalidOperationException("SKU and product name are required.");
            var product = _selectedId.HasValue ? await LoadTrackedAsync(_selectedId.Value) : new Product { Id = Guid.NewGuid() };
            product.Sku = SkuBox.Text.Trim(); product.Name = NameBox.Text.Trim(); product.NameArabic = ArabicNameBox.Text.Trim(); product.IsActive = true;
            if (Guid.TryParse(CategoryBox.Text, out var categoryId)) product.CategoryId = categoryId;
            if (_units.Count == 0) throw new InvalidOperationException("A product must have at least one unit.");
            _conversion.ValidateProductUnits(productWithUnits(product));
            var baseUnit = _units.Single(x => x.IsBaseUnit);
            product.BaseUnitId = baseUnit.Id;
            if (product.BaseUnitId == Guid.Empty) { baseUnit.Id = Guid.NewGuid(); product.BaseUnitId = baseUnit.Id; }
            foreach (var unit in _units) { unit.ProductId = product.Id; if (unit.Id == Guid.Empty) unit.Id = Guid.NewGuid(); }
            await using var db = await _dbFactory.CreateDbContextAsync();
            if (_selectedId is null) { db.Products.Add(product); db.ProductUnits.AddRange(_units); }
            else { var existing = await db.Products.SingleAsync(x => x.Id == product.Id); existing.Sku = product.Sku; existing.Name = product.Name; existing.NameArabic = product.NameArabic; existing.CategoryId = product.CategoryId; existing.BaseUnitId = product.BaseUnitId; db.ProductUnits.UpdateRange(_units); }
            await db.SaveChangesAsync(); await LoadAsync(); StatusText.Foreground = System.Windows.Media.Brushes.DarkGreen; StatusText.Text = "Saved.";
        }
        catch (Exception ex) { StatusText.Foreground = System.Windows.Media.Brushes.DarkRed; StatusText.Text = ex.Message; }
    }
    private async Task<Product> LoadTrackedAsync(Guid id)
    { await using var db = await _dbFactory.CreateDbContextAsync(); var product = await db.Products.SingleAsync(x => x.Id == id); product.Units = _units; return product; }
    private Product productWithUnits(Product product) { product.Units = _units; return product; }
    private sealed record ProductListItem(Guid Id, string Sku, string Name, string? NameArabic);
}
