using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Shapes;
using Microsoft.EntityFrameworkCore;
using POS_WPF.Domain.Products;
using POS_WPF.Domain.Sales;

namespace POS_WPF;

public partial class PosWindow
{
    private static readonly bool _inventoryGuardRegistered = RegisterInventoryGuardHandlers();
    private bool _inventoryCardRefreshRunning;

    private static bool RegisterInventoryGuardHandlers()
    {
        EventManager.RegisterClassHandler(typeof(PosWindow), FrameworkElement.LoadedEvent, new RoutedEventHandler(OnInventoryGuardLoaded));
        EventManager.RegisterClassHandler(typeof(PosWindow), KeyDownEvent, new KeyEventHandler(OnPosBarcodeKeyDown), true);
        return true;
    }

    private static void OnInventoryGuardLoaded(object sender, RoutedEventArgs e)
    {
        if (sender is not PosWindow window) return;
        window.Dispatcher.BeginInvoke(new Action(() => window.InitializeInventoryAwareProductCards()), System.Windows.Threading.DispatcherPriority.Background);
    }

    private void InitializeInventoryAwareProductCards()
    {
        if (PopularProductsPanel is null) return;
        ApplyInventoryAwareProductTemplate();
        DependencyPropertyDescriptorHelper.Attach(PopularProductsPanel, ItemsControl.ItemsSourceProperty, async () => await RefreshProductCardsForStockAsync());
        _ = RefreshProductCardsForStockAsync();
    }

    private async Task RefreshProductCardsForStockAsync()
    {
        if (_inventoryCardRefreshRunning || PopularProductsPanel is null) return;
        _inventoryCardRefreshRunning = true;
        try
        {
            if (PopularProductsPanel.ItemsSource is not System.Collections.IEnumerable source) return;
            var items = source.Cast<object>().OfType<PopularProduct>().ToList();
            if (items.Count == 0) return;

            await using var db = await _dbFactory.CreateDbContextAsync();
            var warehouse = await db.Warehouses.AsNoTracking().Where(x => x.IsActive).OrderBy(x => x.Code).FirstOrDefaultAsync();
            if (warehouse is null) return;

            var ids = items.Select(x => x.ProductId).Distinct().ToList();
            var stock = await db.InventoryTransactions.AsNoTracking()
                .Where(x => ids.Contains(x.ProductId) && x.WarehouseId == warehouse.Id)
                .GroupBy(x => x.ProductId)
                .Select(g => new { ProductId = g.Key, Quantity = g.Sum(x => x.BaseQuantity) })
                .ToDictionaryAsync(x => x.ProductId, x => x.Quantity);

            var visible = items.Where(x => stock.GetValueOrDefault(x.ProductId) > 0m).ToList();
            if (visible.Count != items.Count)
                PopularProductsPanel.ItemsSource = visible;
        }
        catch (Exception ex)
        {
            Status(ex.InnerException?.Message ?? ex.Message, false);
        }
        finally
        {
            _inventoryCardRefreshRunning = false;
        }
    }

    private void ApplyInventoryAwareProductTemplate()
    {
        var template = new DataTemplate();
        var root = new FrameworkElementFactory(typeof(Button));
        root.SetValue(FrameworkElement.WidthProperty, 296d);
        root.SetValue(FrameworkElement.HeightProperty, 108d);
        root.SetValue(FrameworkElement.MarginProperty, new Thickness(0, 0, 8, 10));
        root.SetValue(Control.PaddingProperty, new Thickness(12));
        root.SetValue(Control.HorizontalContentAlignmentProperty, HorizontalAlignment.Stretch);
        root.SetValue(Control.VerticalContentAlignmentProperty, VerticalAlignment.Stretch);
        root.SetValue(Control.BackgroundProperty, Brushes.White);
        root.SetValue(Control.BorderBrushProperty, new SolidColorBrush(Color.FromRgb(215, 222, 232)));
        root.SetValue(Control.BorderThicknessProperty, new Thickness(1));
        root.AddHandler(ButtonBase.ClickEvent, new RoutedEventHandler(InventoryProductCard_Click));

        var borderTemplate = new ControlTemplate(typeof(Button));
        var border = new FrameworkElementFactory(typeof(Border));
        border.SetBinding(Border.BackgroundProperty, new Binding(nameof(Control.Background)) { RelativeSource = new RelativeSource(RelativeSourceMode.TemplatedParent) });
        border.SetBinding(Border.BorderBrushProperty, new Binding(nameof(Control.BorderBrush)) { RelativeSource = new RelativeSource(RelativeSourceMode.TemplatedParent) });
        border.SetBinding(Border.BorderThicknessProperty, new Binding(nameof(Control.BorderThickness)) { RelativeSource = new RelativeSource(RelativeSourceMode.TemplatedParent) });
        border.SetValue(Border.CornerRadiusProperty, new CornerRadius(5));
        border.SetValue(Border.PaddingProperty, new Thickness(12));
        var presenter = new FrameworkElementFactory(typeof(ContentPresenter));
        presenter.SetBinding(ContentPresenter.ContentProperty, new Binding(nameof(ContentControl.Content)) { RelativeSource = new RelativeSource(RelativeSourceMode.TemplatedParent) });
        presenter.SetBinding(ContentPresenter.HorizontalAlignmentProperty, new Binding(nameof(Control.HorizontalContentAlignment)) { RelativeSource = new RelativeSource(RelativeSourceMode.TemplatedParent) });
        presenter.SetBinding(ContentPresenter.VerticalAlignmentProperty, new Binding(nameof(Control.VerticalContentAlignment)) { RelativeSource = new RelativeSource(RelativeSourceMode.TemplatedParent) });
        border.AppendChild(presenter);
        borderTemplate.VisualTree = border;
        root.SetValue(Control.TemplateProperty, borderTemplate);

        var content = new FrameworkElementFactory(typeof(Grid));
        content.SetValue(FrameworkElement.IsHitTestVisibleProperty, true);
        content.AppendChild(BoundTextBlock(nameof(PopularProduct.SkuText), 0, 0, 8, new SolidColorBrush(Color.FromRgb(148, 163, 184))));
        content.AppendChild(BoundTextBlock(nameof(PopularProduct.Name), 1, 0, 12, new SolidColorBrush(Color.FromRgb(23, 32, 51)), FontWeights.SemiBold));

        var pricePanel = new FrameworkElementFactory(typeof(StackPanel));
        pricePanel.SetValue(StackPanel.OrientationProperty, Orientation.Horizontal);
        pricePanel.SetValue(Grid.RowProperty, 2);
        var price = BoundTextBlock(nameof(PopularProduct.PriceText), 0, 0, 15, new SolidColorBrush(Color.FromRgb(22, 163, 74)), FontWeights.Bold);
        pricePanel.AppendChild(price);
        var unit = BoundTextBlock(nameof(PopularProduct.UnitName), 0, 0, 8, new SolidColorBrush(Color.FromRgb(148, 163, 184)));
        unit.SetValue(FrameworkElement.MarginProperty, new Thickness(4, 4, 0, 0));
        pricePanel.AppendChild(unit);
        content.AppendChild(pricePanel);

        var info = new FrameworkElementFactory(typeof(ProductCardStockInfo));
        info.SetValue(Grid.RowProperty, 3);
        info.SetValue(FrameworkElement.MarginProperty, new Thickness(0, 3, 0, 0));
        info.SetBinding(ProductCardStockInfo.ProductIdProperty, new Binding(nameof(PopularProduct.ProductId)));
        content.AppendChild(info);

        var rows = new FrameworkElementFactory(typeof(RowDefinition));
        rows.SetValue(RowDefinition.HeightProperty, new GridLength(1, GridUnitType.Auto));
        content.AppendChild(rows);
        root.SetValue(ContentControl.ContentProperty, content);
        template.VisualTree = root;
        template.Seal();
        PopularProductsPanel.ItemTemplate = template;
        PopularProductsPanel.ItemContainerStyle = null;
    }

    private static FrameworkElementFactory BoundTextBlock(string property, int row, int column, double fontSize, Brush foreground, FontWeight? weight = null)
    {
        var text = new FrameworkElementFactory(typeof(TextBlock));
        text.SetBinding(TextBlock.TextProperty, new Binding(property));
        text.SetValue(Grid.RowProperty, row);
        text.SetValue(Grid.ColumnProperty, column);
        text.SetValue(Control.FontSizeProperty, fontSize);
        text.SetValue(Control.ForegroundProperty, foreground);
        if (weight.HasValue) text.SetValue(Control.FontWeightProperty, weight.Value);
        text.SetValue(TextBlock.TextTrimmingProperty, TextTrimming.CharacterEllipsis);
        return text;
    }

    private async void InventoryProductCard_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not FrameworkElement { DataContext: PopularProduct item }) return;
        e.Handled = true;
        await AddPopularProductWithStockGuardAsync(item);
    }

    private async Task AddPopularProductWithStockGuardAsync(PopularProduct item)
    {
        try
        {
            if (item.UnitId == Guid.Empty) throw new InvalidOperationException("The product has no base selling unit.");
            var existing = _cart.FirstOrDefault(x => x.ProductId == item.ProductId && x.UnitId == item.UnitId);
            var requested = (existing?.Quantity ?? 0m) + 1m;
            if (!await HasSufficientStockAsync(item.ProductId, item.UnitId, requested))
            {
                Status("المخزون لا يكفي للإضافة.", false);
                return;
            }
            if (existing is null)
            {
                existing = new CartItem(item.ProductId, item.UnitId, item.Name, item.UnitName, 1m, item.Price);
                _cart.Add(existing);
            }
            else existing.Quantity = requested;
            await ApplyPromotionAsync(existing);
            RefreshTotal();
            Status($"Added {item.Name}.", true);
        }
        catch (Exception ex) { Status(ex.InnerException?.Message ?? ex.Message, false); }
    }

    private static void OnPosBarcodeKeyDown(object sender, KeyEventArgs e)
    {
        if (sender is not PosWindow window || e.OriginalSource is not TextBox textBox || !ReferenceEquals(textBox, window.BarcodeBox) || e.Key != Key.Enter) return;
        e.Handled = true;
        _ = window.AddBarcodeWithStockGuardAsync();
    }

    private async Task AddBarcodeWithStockGuardAsync()
    {
        var code = BarcodeBox.Text.Trim();
        if (code.Length == 0) return;
        try
        {
            var match = await _barcode.FindAsync(code);
            if (match is null) { Status("Barcode not found.", false); return; }
            if (!match.Unit.CanSell) { Status("This unit is not sellable.", false); return; }
            var existing = _cart.FirstOrDefault(x => x.ProductId == match.Product.Id && x.UnitId == match.Unit.Id);
            var requested = (existing?.Quantity ?? 0m) + 1m;
            if (!await HasSufficientStockAsync(match.Product.Id, match.Unit.Id, requested))
            {
                Status("المخزون لا يكفي للإضافة.", false);
                return;
            }
            if (existing is null)
            {
                existing = new CartItem(match.Product.Id, match.Unit.Id, match.Product.Name, match.Unit.Name, match.Unit.ConversionFactorToBase, match.Unit.SellingPrice);
                _cart.Add(existing);
            }
            else existing.Quantity = requested;
            await ApplyPromotionAsync(existing);
            RefreshTotal();
            Status($"Added {match.Product.Name} ({match.Unit.Abbreviation}).", true);
        }
        catch (Exception ex) { Status(ex.InnerException?.Message ?? ex.Message, false); }
        finally { BarcodeBox.Clear(); BarcodeBox.Focus(); }
    }

    private async Task<bool> HasSufficientStockAsync(Guid productId, Guid unitId, decimal requestedQuantity)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var warehouse = await db.Warehouses.AsNoTracking().Where(x => x.IsActive).OrderBy(x => x.Code).FirstOrDefaultAsync();
        if (warehouse is null) return false;
        var stock = await db.InventoryTransactions.AsNoTracking().Where(x => x.ProductId == productId && x.WarehouseId == warehouse.Id).SumAsync(x => (decimal?)x.BaseQuantity) ?? 0m;
        var factor = await db.ProductUnits.AsNoTracking().Where(x => x.Id == unitId && x.ProductId == productId).Select(x => (decimal?)x.ConversionFactorToBase).SingleOrDefaultAsync() ?? 1m;
        return requestedQuantity * factor <= stock + 0.000001m;
    }

    private async Task<(decimal Stock, decimal Reorder, string Barcode, string Unit)> GetProductCardInfoAsync(Guid productId)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var product = await db.Products.AsNoTracking().Include(x => x.Units).SingleOrDefaultAsync(x => x.Id == productId);
        if (product is null) return (0m, 0m, string.Empty, "PCS");
        var warehouse = await db.Warehouses.AsNoTracking().Where(x => x.IsActive).OrderBy(x => x.Code).FirstOrDefaultAsync();
        var stock = warehouse is null ? 0m : await db.InventoryTransactions.AsNoTracking().Where(x => x.ProductId == productId && x.WarehouseId == warehouse.Id).SumAsync(x => (decimal?)x.BaseQuantity) ?? 0m;
        var unit = product.Units.SingleOrDefault(x => x.Id == product.BaseUnitId) ?? product.Units.SingleOrDefault(x => x.IsBaseUnit);
        return (stock, product.ReorderLevel, unit?.Barcode ?? string.Empty, unit?.Abbreviation ?? "PCS");
    }

    private sealed class ProductCardStockInfo : StackPanel
    {
        public static readonly DependencyProperty ProductIdProperty = DependencyProperty.Register(nameof(ProductId), typeof(Guid), typeof(ProductCardStockInfo), new PropertyMetadata(Guid.Empty, OnProductIdChanged));
        public Guid ProductId { get => (Guid)GetValue(ProductIdProperty); set => SetValue(ProductIdProperty, value); }
        private readonly TextBlock _text = new() { FontSize = 8, Foreground = new SolidColorBrush(Color.FromRgb(100, 116, 139)) };
        private readonly Ellipse _indicator = new() { Width = 8, Height = 8, Margin = new Thickness(0, 0, 4, 0) };
        public ProductCardStockInfo()
        {
            Orientation = Orientation.Horizontal;
            Children.Add(_indicator);
            Children.Add(_text);
            Loaded += async (_, _) => await RefreshAsync();
        }
        private static void OnProductIdChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) { if (d is ProductCardStockInfo control && control.IsLoaded) _ = control.RefreshAsync(); }
        private async Task RefreshAsync()
        {
            if (ProductId == Guid.Empty || FindParentWindow(this) is not PosWindow window) return;
            var info = await window.GetProductCardInfoAsync(ProductId);
            var state = info.Stock <= info.Reorder && info.Reorder > 0m ? "LOW" : info.Reorder > 0m && info.Stock <= info.Reorder * 2m ? "WATCH" : "IN STOCK";
            _text.Text = $"Barcode: {(string.IsNullOrWhiteSpace(info.Barcode) ? "—" : info.Barcode)}   Stock: {info.Stock:N0} {info.Unit}   {state}";
            _indicator.Fill = state == "LOW" ? Brushes.Red : state == "WATCH" ? Brushes.Gold : Brushes.Green;
        }
        private static Window? FindParentWindow(DependencyObject child)
        {
            var current = child;
            while (current is not null)
            {
                if (current is Window window) return window;
                current = LogicalTreeHelper.GetParent(current) ?? (current is Visual visual ? VisualTreeHelper.GetParent(visual) : null);
            }
            return null;
        }
    }

    private static class DependencyPropertyDescriptorHelper
    {
        public static void Attach(DependencyObject source, DependencyProperty property, Func<Task> callback)
        {
            var descriptor = System.ComponentModel.DependencyPropertyDescriptor.FromProperty(property, source.GetType());
            if (descriptor is null) return;
            descriptor.AddValueChanged(source, (_, _) => _ = callback());
        }
    }
}
