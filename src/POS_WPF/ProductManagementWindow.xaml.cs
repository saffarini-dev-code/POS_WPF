using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using Microsoft.EntityFrameworkCore;
using POS_WPF.Data;
using POS_WPF.Domain.Audit;
using POS_WPF.Domain.Inventory;
using POS_WPF.Domain.Products;
using POS_WPF.Infrastructure.Security;

namespace POS_WPF;

public partial class ProductManagementWindow : UserControl
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
        _dbFactory = dbFactory;
        _conversion = conversion;
        _inventory = inventory;
        _permissions = permissions;
        _session = session;
        UnitsGrid.ItemsSource = _units;
        Loaded += async (_, _) =>
        {
            await LoadCategoriesAsync();
            await LoadWarehousesAsync();
            await LoadAsync();
            NewProduct(false);
        };
    }

    private async Task LoadCategoriesAsync()
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        CategoryBox.ItemsSource = await db.Categories.AsNoTracking().Where(x => x.IsActive).OrderBy(x => x.Name).ToListAsync();
    }

    private async Task LoadWarehousesAsync()
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        WarehouseBox.ItemsSource = await db.Warehouses.AsNoTracking().Where(x => x.IsActive).OrderBy(x => x.Code).Select(x => new WarehouseOption(x.Id, x.Code + " — " + x.Name)).ToListAsync();
    }

    private async Task LoadAsync()
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        _products = await db.Products.AsNoTracking().OrderBy(x => x.Name).Select(x => new ProductListItem(x.Id, x.Sku, x.Name, x.NameArabic, db.Categories.Where(c => c.Id == x.CategoryId).Select(c => c.Name).FirstOrDefault())).ToListAsync();
        ProductsGrid.ItemsSource = _products;
    }

    private async void Refresh_Click(object sender, RoutedEventArgs e)
    {
        await LoadCategoriesAsync();
        await LoadWarehousesAsync();
        await LoadAsync();
        Status("Catalog refreshed.", true);
    }

    private void Search_Changed(object sender, TextChangedEventArgs e)
    {
        var term = SearchBox.Text.Trim();
        ProductsGrid.ItemsSource = string.IsNullOrEmpty(term) ? _products : _products.Where(x => x.Name.Contains(term, StringComparison.OrdinalIgnoreCase) || x.Sku.Contains(term, StringComparison.OrdinalIgnoreCase) || (x.CategoryName?.Contains(term, StringComparison.OrdinalIgnoreCase) ?? false)).ToList();
    }

    private void New_Click(object sender, RoutedEventArgs e) => NewProduct();

    private void NewProduct(bool showStatus = true)
    {
        _selectedId = null;
        ProductsGrid.SelectedItem = null;
        SkuBox.Clear();
        NameBox.Clear();
        ArabicNameBox.Clear();
        CategoryBox.SelectedValue = null;
        WarehouseBox.SelectedIndex = WarehouseBox.Items.Count > 0 ? 0 : -1;
        OpeningStockBox.Text = "0";
        OpeningStockBox.IsEnabled = true;
        _units.Clear();
        AddBaseUnit();
        CurrentStockText.Text = "0";
        StockUnitText.Text = " PCS";
        if (showStatus) Status("Ready for a new product.", true);
        SkuBox.Focus();
    }

    private void AddBaseUnit()
    {
        _units.Add(new ProductUnit
        {
            Id = Guid.NewGuid(),
            Name = "PCS",
            Abbreviation = "PCS",
            ConversionFactorToBase = 1m,
            IsBaseUnit = true,
            CanSell = true,
            CanPurchase = true,
            IsActive = true
        });
    }

    private async void Product_Selected(object sender, SelectionChangedEventArgs e)
    {
        if (ProductsGrid.SelectedItem is not ProductListItem item) return;
        try
        {
            _selectedId = item.Id;
            SkuBox.Text = item.Sku;
            NameBox.Text = item.Name;
            ArabicNameBox.Text = item.NameArabic ?? string.Empty;
            OpeningStockBox.Text = "0";
            OpeningStockBox.IsEnabled = false;
            await using var db = await _dbFactory.CreateDbContextAsync();
            var product = await db.Products.AsNoTracking().Include(x => x.Units).SingleAsync(x => x.Id == item.Id);
            CategoryBox.SelectedValue = product.CategoryId;
            _units.Clear();
            foreach (var unit in product.Units.OrderByDescending(x => x.IsBaseUnit).ThenBy(x => x.ConversionFactorToBase).ThenBy(x => x.Name))
            {
                _units.Add(new ProductUnit
                {
                    Id = unit.Id,
                    ProductId = unit.ProductId,
                    Name = unit.Name,
                    Abbreviation = unit.Abbreviation,
                    Barcode = unit.Barcode,
                    ConversionFactorToBase = unit.ConversionFactorToBase,
                    PurchasePrice = unit.PurchasePrice,
                    SellingPrice = unit.SellingPrice,
                    WholesalePrice = unit.WholesalePrice,
                    WholesaleWholesalePrice = unit.WholesaleWholesalePrice,
                    IsBaseUnit = unit.IsBaseUnit,
                    CanSell = unit.CanSell,
                    CanPurchase = unit.CanPurchase,
                    IsActive = unit.IsActive
                });
            }
            var warehouse = await db.Warehouses.AsNoTracking().Where(x => x.IsActive).OrderBy(x => x.Code).FirstOrDefaultAsync();
            var stock = warehouse is null ? 0m : await _inventory.GetBaseBalanceAsync(product.Id, warehouse.Id);
            var baseUnit = product.Units.SingleOrDefault(x => x.Id == product.BaseUnitId) ?? product.Units.SingleOrDefault(x => x.IsBaseUnit);
            if (warehouse is not null) WarehouseBox.SelectedValue = warehouse.Id;
            CurrentStockText.Text = stock.ToString("N3");
            StockUnitText.Text = $" {baseUnit?.Abbreviation ?? "PCS"}";
            StatusText.Foreground = System.Windows.Media.Brushes.DimGray;
            StatusText.Text = "Edit mode — change any product or unit field and save.";
        }
        catch (Exception ex) { Status(ex.InnerException?.Message ?? ex.Message, false); }
    }

    private void AddUnit_Click(object sender, RoutedEventArgs e)
    {
        if (_units.Count == 0) { AddBaseUnit(); return; }
        _units.Add(new ProductUnit
        {
            Id = Guid.NewGuid(),
            ProductId = _selectedId ?? Guid.Empty,
            Name = string.Empty,
            Abbreviation = string.Empty,
            ConversionFactorToBase = 1m,
            IsBaseUnit = false,
            CanSell = true,
            CanPurchase = true,
            IsActive = true
        });
        UnitsGrid.SelectedIndex = _units.Count - 1;
        UnitsGrid.ScrollIntoView(_units[^1]);
    }

    private void RemoveUnit_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not FrameworkElement { DataContext: ProductUnit unit }) return;
        if (unit.IsBaseUnit)
        {
            Status("The base unit cannot be removed. Select another unit as base first.", false);
            return;
        }
        _units.Remove(unit);
        Status("Unit removed from the product. Save to apply the change.", true);
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
            if (!decimal.TryParse(OpeningStockBox.Text, NumberStyles.Number, CultureInfo.CurrentCulture, out var openingStock) || openingStock < 0) throw new InvalidOperationException("Opening stock must be a valid non-negative number.");

            Guid? warehouseId = null;
            if (!_selectedId.HasValue)
            {
                if (WarehouseBox.SelectedValue is not Guid selectedWarehouseId) throw new InvalidOperationException("Select the opening-stock warehouse.");
                warehouseId = selectedWarehouseId;
            }

            var baseUnit = _units.Single(x => x.IsBaseUnit);
            if (string.IsNullOrWhiteSpace(baseUnit.Name) || string.IsNullOrWhiteSpace(baseUnit.Abbreviation)) throw new InvalidOperationException("The base unit needs a name and abbreviation.");
            if (baseUnit.ConversionFactorToBase != 1m) throw new InvalidOperationException("The base unit conversion factor must be exactly 1.00.");

            foreach (var unit in _units)
            {
                unit.Name = unit.Name.Trim();
                unit.Abbreviation = unit.Abbreviation.Trim();
                unit.Barcode = string.IsNullOrWhiteSpace(unit.Barcode) ? null : unit.Barcode.Trim();
            }

            var duplicateName = _units.GroupBy(x => x.Name, StringComparer.OrdinalIgnoreCase).FirstOrDefault(g => g.Count() > 1);
            if (duplicateName is not null) throw new InvalidOperationException($"Duplicate unit name '{duplicateName.Key}'. Each product can define a unit only once.");

            var duplicateAbbreviation = _units.GroupBy(x => x.Abbreviation, StringComparer.OrdinalIgnoreCase).FirstOrDefault(g => g.Count() > 1);
            if (duplicateAbbreviation is not null) throw new InvalidOperationException($"Duplicate unit abbreviation '{duplicateAbbreviation.Key}'. Each product unit needs a unique abbreviation.");

            foreach (var unit in _units)
            {
                if (string.IsNullOrWhiteSpace(unit.Name) || string.IsNullOrWhiteSpace(unit.Abbreviation)) throw new InvalidOperationException("Every unit needs a name and abbreviation.");
                if (unit.ConversionFactorToBase <= 0) throw new InvalidOperationException($"Invalid conversion factor for '{unit.Name}'. It must be greater than zero.");
                if (unit.PurchasePrice < 0 || unit.SellingPrice < 0 || unit.WholesalePrice < 0 || unit.WholesaleWholesalePrice < 0) throw new InvalidOperationException($"Prices cannot be negative for '{unit.Name}'.");
                if (unit.SellingPrice < unit.PurchasePrice) throw new InvalidOperationException($"Retail price for '{unit.Name}' must be greater than or equal to cost price.");
                if (unit.WholesalePrice > 0 && unit.WholesalePrice < unit.PurchasePrice) throw new InvalidOperationException($"Wholesale price for '{unit.Name}' must be greater than or equal to cost price, or left empty.");
                if (unit.WholesaleWholesalePrice > 0 && unit.WholesaleWholesalePrice < unit.PurchasePrice) throw new InvalidOperationException($"Wholesale+ price for '{unit.Name}' must be greater than or equal to cost price, or left empty.");
            }

            var productId = _selectedId ?? Guid.NewGuid();
            var isNew = !_selectedId.HasValue;
            await using var db = await _dbFactory.CreateDbContextAsync();

            if (isNew)
            {
                var product = new Product
                {
                    Id = productId,
                    Sku = SkuBox.Text.Trim(),
                    Name = NameBox.Text.Trim(),
                    NameArabic = string.IsNullOrWhiteSpace(ArabicNameBox.Text) ? null : ArabicNameBox.Text.Trim(),
                    CategoryId = categoryId,
                    IsActive = true,
                    BaseUnitId = baseUnit.Id
                };
                _conversion.ValidateProductUnits(product);
                db.Products.Add(product);
                foreach (var unit in _units) { unit.ProductId = productId; unit.Product = null!; }
                db.ProductUnits.AddRange(_units);

                if (openingStock > 0 && warehouseId.HasValue)
                {
                    var branchId = await db.Warehouses.Where(x => x.Id == warehouseId.Value).Select(x => x.BranchId).SingleAsync();
                    db.InventoryTransactions.Add(new InventoryTransaction
                    {
                        ProductId = productId,
                        UnitId = baseUnit.Id,
                        TransactionQuantity = openingStock,
                        ConversionFactor = baseUnit.ConversionFactorToBase,
                        BaseQuantity = openingStock * baseUnit.ConversionFactorToBase,
                        TransactionType = InventoryTransactionType.OpeningStock,
                        Reference = $"OPEN-{product.Sku}",
                        WarehouseId = warehouseId.Value,
                        BranchId = branchId,
                        UserId = _session.CurrentUser?.Id
                    });
                }
            }
            else
            {
                var existing = await db.Products.SingleAsync(x => x.Id == productId);
                existing.Sku = SkuBox.Text.Trim();
                existing.Name = NameBox.Text.Trim();
                existing.NameArabic = string.IsNullOrWhiteSpace(ArabicNameBox.Text) ? null : ArabicNameBox.Text.Trim();
                existing.CategoryId = categoryId;
                existing.BaseUnitId = baseUnit.Id;

                var existingUnits = await db.ProductUnits.Where(x => x.ProductId == productId).ToListAsync();
                var incomingIds = _units.Select(x => x.Id).ToHashSet();
                db.ProductUnits.RemoveRange(existingUnits.Where(x => !incomingIds.Contains(x.Id)));

                foreach (var incoming in _units)
                {
                    var tracked = existingUnits.FirstOrDefault(x => x.Id == incoming.Id);
                    if (tracked is null)
                    {
                        incoming.ProductId = productId;
                        incoming.Product = null!;
                        db.ProductUnits.Add(incoming);
                    }
                    else
                    {
                        tracked.Name = incoming.Name;
                        tracked.Abbreviation = incoming.Abbreviation;
                        tracked.Barcode = incoming.Barcode;
                        tracked.ConversionFactorToBase = incoming.ConversionFactorToBase;
                        tracked.PurchasePrice = incoming.PurchasePrice;
                        tracked.SellingPrice = incoming.SellingPrice;
                        tracked.WholesalePrice = incoming.WholesalePrice;
                        tracked.WholesaleWholesalePrice = incoming.WholesaleWholesalePrice;
                        tracked.IsBaseUnit = incoming.IsBaseUnit;
                        tracked.CanSell = incoming.CanSell;
                        tracked.CanPurchase = incoming.CanPurchase;
                        tracked.IsActive = incoming.IsActive;
                    }
                }
            }

            db.AuditEntries.Add(new AuditEntry(_session.CurrentUser?.Id, isNew ? "Product.Created" : "Product.Updated", nameof(Product), productId, null, SkuBox.Text.Trim(), null, null, "Product Management"));
            await db.SaveChangesAsync();
            await LoadAsync();
            NewProduct(false);
            Status(isNew ? "Product saved successfully. Form is ready for the next product." : "Product updated successfully. Form is ready for the next product.", true);
        }
        catch (DbUpdateException ex)
        {
            var message = ex.InnerException?.Message ?? ex.Message;
            if (message.Contains("ProductUnits.ProductId, ProductUnits.Name", StringComparison.OrdinalIgnoreCase))
                message = "Each product can have only one unit with the same name. Define the second unit as a different name such as Carton or Box.";
            Status(message, false);
        }
        catch (Exception ex) { Status(ex.InnerException?.Message ?? ex.Message, false); }
    }

    private void Status(string message, bool success)
    {
        StatusText.Foreground = success ? System.Windows.Media.Brushes.DarkGreen : System.Windows.Media.Brushes.DarkRed;
        StatusText.Text = success ? "✓  " + message : "⚠  " + message;
    }

    private sealed record ProductListItem(Guid Id, string Sku, string Name, string? NameArabic, string? CategoryName);
    private sealed record WarehouseOption(Guid Id, string Display);
}