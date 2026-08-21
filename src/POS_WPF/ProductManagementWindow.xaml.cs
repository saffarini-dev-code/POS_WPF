using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using Microsoft.EntityFrameworkCore;
using POS_WPF.Data;
using POS_WPF.Domain.Audit;
using POS_WPF.Domain.Inventory;
using POS_WPF.Domain.Products;
using POS_WPF.Infrastructure.Security;

namespace POS_WPF;

public partial class ProductManagementWindow : Window
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;
    private readonly UnitConversionService _conversion;
    private readonly InventoryBalanceService _inventory;
    private readonly PermissionService _permissions;
    private readonly SessionContext _session;
    private Guid? _selectedId;
    private readonly ObservableCollection<ProductUnit> _units = [];
    private List<ProductListItem> _products = [];

    public ProductManagementWindow(IDbContextFactory<AppDbContext> dbFactory, UnitConversionService conversion, InventoryBalanceService inventory, PermissionService permissions, SessionContext session)
    {
        InitializeComponent();
        _dbFactory = dbFactory; _conversion = conversion; _inventory = inventory; _permissions = permissions; _session = session;
        UnitsGrid.ItemsSource = _units;
        Loaded += async (_, _) => { await LoadCategoriesAsync(); await LoadAsync(); NewProduct(); };
    }

    private async Task LoadCategoriesAsync()
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        CategoryBox.ItemsSource = await db.Categories.AsNoTracking().Where(x => x.IsActive).OrderBy(x => x.Name).ToListAsync();
    }

    private async Task LoadAsync()
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        _products = await db.Products.AsNoTracking().OrderBy(x => x.Name).Select(x => new ProductListItem(x.Id, x.Sku, x.Name, x.NameArabic, db.Categories.Where(c => c.Id == x.CategoryId).Select(c => c.Name).FirstOrDefault())).ToListAsync();
        ProductsGrid.ItemsSource = _products;
    }

    private async void Refresh_Click(object sender, RoutedEventArgs e) { await LoadCategoriesAsync(); await LoadAsync(); }

    private void Search_Changed(object sender, TextChangedEventArgs e)
    {
        var term = SearchBox.Text.Trim();
        ProductsGrid.ItemsSource = string.IsNullOrEmpty(term) ? _products : _products.Where(x => x.Name.Contains(term, StringComparison.OrdinalIgnoreCase) || x.Sku.Contains(term, StringComparison.OrdinalIgnoreCase) || (x.CategoryName?.Contains(term, StringComparison.OrdinalIgnoreCase) ?? false)).ToList();
    }

    private void New_Click(object sender, RoutedEventArgs e) => NewProduct();

    private void NewProduct()
    {
        _selectedId = null; ProductsGrid.SelectedItem = null; SkuBox.Clear(); NameBox.Clear(); ArabicNameBox.Clear(); CategoryBox.SelectedValue = null; _units.Clear(); AddUnit_Click(this, new RoutedEventArgs()); CurrentStockText.Text = "0"; StockUnitText.Text = " PCS"; StatusText.Foreground = System.Windows.Media.Brushes.DarkGreen; StatusText.Text = "New product.";
    }

    private async void Product_Selected(object sender, SelectionChangedEventArgs e)
    {
        if (ProductsGrid.SelectedItem is not ProductListItem item) return;
        try
        {
            _selectedId = item.Id; SkuBox.Text = item.Sku; NameBox.Text = item.Name; ArabicNameBox.Text = item.NameArabic ?? string.Empty;
            await using var db = await _dbFactory.CreateDbContextAsync();
            var product = await db.Products.AsNoTracking().Include(x => x.Units).SingleAsync(x => x.Id == item.Id);
            CategoryBox.SelectedValue = product.CategoryId;
            _units.Clear(); foreach (var unit in product.Units.OrderByDescending(x => x.IsBaseUnit).ThenBy(x => x.Name)) _units.Add(unit);
            var warehouse = await db.Warehouses.AsNoTracking().Where(x => x.IsActive).OrderBy(x => x.Code).FirstOrDefaultAsync();
            var stock = warehouse is null ? 0m : await _inventory.GetBaseBalanceAsync(product.Id, warehouse.Id);
            var baseUnit = product.Units.SingleOrDefault(x => x.Id == product.BaseUnitId) ?? product.Units.SingleOrDefault(x => x.IsBaseUnit);
            CurrentStockText.Text = stock.ToString("N3"); StockUnitText.Text = $" {baseUnit?.Abbreviation ?? "PCS"}";
            StatusText.Foreground = System.Windows.Media.Brushes.DimGray; StatusText.Text = string.Empty;
        }
        catch (Exception ex) { StatusText.Foreground = System.Windows.Media.Brushes.DarkRed; StatusText.Text = ex.Message; }
    }

    private void AddUnit_Click(object sender, RoutedEventArgs e)
    {
        _units.Add(new ProductUnit { Name = "PCS", Abbreviation = "PCS", ConversionFactorToBase = 1m, IsBaseUnit = _units.Count == 0, CanSell = true, CanPurchase = true, IsActive = true });
    }

    private async void Save_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            await _permissions.DemandAsync(_selectedId.HasValue ? "Products.Edit" : "Products.Create");
            if (string.IsNullOrWhiteSpace(SkuBox.Text) || string.IsNullOrWhiteSpace(NameBox.Text)) throw new InvalidOperationException("SKU and product name are required.");
            if (_units.Count == 0) throw new InvalidOperationException("A product must have at least one unit.");
            if (CategoryBox.SelectedValue is not Guid categoryId) throw new InvalidOperationException("Select a product category.");
            if (_units.Count(x => x.IsBaseUnit) != 1) throw new InvalidOperationException("Exactly one unit must be the base unit.");
            foreach (var unit in _units)
            {
                if (string.IsNullOrWhiteSpace(unit.Name) || string.IsNullOrWhiteSpace(unit.Abbreviation)) throw new InvalidOperationException("Every unit needs a name and abbreviation.");
                if (unit.ConversionFactorToBase <= 0) throw new InvalidOperationException($"Invalid conversion factor for {unit.Name}.");
                if (unit.PurchasePrice < 0 || unit.SellingPrice < 0 || unit.WholesalePrice < 0 || unit.WholesaleWholesalePrice < 0) throw new InvalidOperationException($"Prices cannot be negative for {unit.Name}.");
            }

            var productId = _selectedId ?? Guid.NewGuid();
            var product = new Product { Id = productId, Sku = SkuBox.Text.Trim(), Name = NameBox.Text.Trim(), NameArabic = ArabicNameBox.Text.Trim(), CategoryId = categoryId, IsActive = true, Units = _units };
            _conversion.ValidateProductUnits(product);
            var baseUnit = _units.Single(x => x.IsBaseUnit);
            if (baseUnit.Id == Guid.Empty) baseUnit.Id = Guid.NewGuid(); product.BaseUnitId = baseUnit.Id;
            foreach (var unit in _units) { if (unit.Id == Guid.Empty) unit.Id = Guid.NewGuid(); unit.ProductId = productId; }

            await using var db = await _dbFactory.CreateDbContextAsync();
            if (_selectedId is null)
            {
                db.Products.Add(new Product { Id = productId, Sku = product.Sku, Name = product.Name, NameArabic = product.NameArabic, CategoryId = categoryId, IsActive = true, BaseUnitId = baseUnit.Id });
                db.ProductUnits.AddRange(_units);
            }
            else
            {
                var existing = await db.Products.SingleAsync(x => x.Id == productId);
                existing.Sku = product.Sku; existing.Name = product.Name; existing.NameArabic = product.NameArabic; existing.CategoryId = categoryId; existing.BaseUnitId = baseUnit.Id;
                var existingUnits = await db.ProductUnits.Where(x => x.ProductId == productId).ToListAsync();
                var keepIds = _units.Select(x => x.Id).ToHashSet();
                db.ProductUnits.RemoveRange(existingUnits.Where(x => !keepIds.Contains(x.Id)));
                foreach (var unit in _units) { unit.Product = null!; db.ProductUnits.Update(unit); }
            }
            db.AuditEntries.Add(new AuditEntry(_session.CurrentUser?.Id, _selectedId is null ? "Product.Created" : "Product.Updated", nameof(Product), productId, null, product.Sku, null, null, "Product Management"));
            await db.SaveChangesAsync(); await LoadAsync();
            StatusText.Foreground = System.Windows.Media.Brushes.DarkGreen; StatusText.Text = "Product saved successfully.";
        }
        catch (Exception ex) { StatusText.Foreground = System.Windows.Media.Brushes.DarkRed; StatusText.Text = ex.InnerException?.Message ?? ex.Message; }
    }

    private sealed record ProductListItem(Guid Id, string Sku, string Name, string? NameArabic, string? CategoryName);
}
