using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Microsoft.EntityFrameworkCore;
using POS_WPF.Domain.Products;
using POS_WPF.Domain.Sales;
using POS_WPF.Domain.Sales;
using POS_WPF.Domain.Sales;
using POS_WPF.Domain.Sales;
using POS_WPF.Domain.Sales;
using POS_WPF.Infrastructure.Security;

namespace POS_WPF;

public partial class PosWindow
{
    private TextBox? _keypadTarget;

    private async void PosWindow_Loaded(object sender, RoutedEventArgs e)
    {
        var user = _session.CurrentUser;
        CurrentUserText.Text = user is null ? "Unknown user" : string.IsNullOrWhiteSpace(user.DisplayName) ? user.Username : user.DisplayName;
        CurrentRoleText.Text = string.IsNullOrWhiteSpace(_session.CurrentRole) ? "Cashier" : _session.CurrentRole;
        CurrentUserInitialsText.Text = GetInitials(user?.DisplayName ?? user?.Username ?? "?");
        _keypadTarget = PaymentBox;
        await LoadCategoryTabsAsync();
    }

    private static string GetInitials(string value)
    {
        var parts = value.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length == 0) return "?";
        if (parts.Length == 1) return parts[0][..Math.Min(2, parts[0].Length)].ToUpperInvariant();
        return $"{parts[0][0]}{parts[^1][0]}".ToUpperInvariant();
    }

    private async Task LoadCategoryTabsAsync()
    {
        CategoryTabsPanel.Children.Clear();
        AddCategoryTab("All", null, true);
        await using var db = await _dbFactory.CreateDbContextAsync();
        var categories = await db.Categories.AsNoTracking().Where(x => x.IsActive).OrderBy(x => x.Name).Select(x => new { x.Id, x.Name }).ToListAsync();
        foreach (var category in categories) AddCategoryTab(category.Name, category.Id, false);
    }

    private void AddCategoryTab(string caption, Guid? categoryId, bool selected)
    {
        var button = new Button { Content = caption, Tag = categoryId, Style = (Style)FindResource("CategoryTabStyle"), Background = selected ? new SolidColorBrush(Color.FromRgb(32, 166, 74)) : Brushes.White, Foreground = selected ? Brushes.White : new SolidColorBrush(Color.FromRgb(51, 65, 85)) };
        button.Click += CategoryTab_Click;
        CategoryTabsPanel.Children.Add(button);
    }

    private async void CategoryTab_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button button) return;
        var categoryId = button.Tag is Guid id ? id : (Guid?)null;
        foreach (var child in CategoryTabsPanel.Children.OfType<Button>())
        {
            var active = ReferenceEquals(child, button);
            child.Background = active ? new SolidColorBrush(Color.FromRgb(32, 166, 74)) : Brushes.White;
            child.Foreground = active ? Brushes.White : new SolidColorBrush(Color.FromRgb(51, 65, 85));
        }

        try
        {
            await LoadProductsForCategoryAsync(categoryId);
        }
        catch (Exception ex)
        {
            Status(ex.InnerException?.Message ?? ex.Message, false);
        }
    }

    private async Task LoadProductsForCategoryAsync(Guid? categoryId)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();

        var sold = await db.Set<SaleLine>()
            .Where(l => db.Sales.Any(s => s.Id == EF.Property<Guid>(l, "SaleId") && s.Status == SaleStatus.Completed))
            .Where(l => db.Products.Any(p => p.Id == l.ProductId && p.IsActive && (!categoryId.HasValue || p.CategoryId == categoryId.Value)))
            .GroupBy(l => l.ProductId)
            .Select(g => new { ProductId = g.Key, Sold = g.Sum(x => x.Quantity * x.ConversionFactor) })
            .OrderByDescending(x => x.Sold)
            .ThenBy(x => x.ProductId)
            .Take(60)
            .ToListAsync();

        var ids = sold.Select(x => x.ProductId).ToList();
        var products = await db.Products.AsNoTracking()
            .Where(x => x.IsActive && (!categoryId.HasValue || x.CategoryId == categoryId.Value))
            .Include(x => x.Units)
            .ToListAsync();

        var soldProducts = sold
            .Join(products, x => x.ProductId, x => x.Id, (x, p) => new { p, x.Sold })
            .Select(x => CreatePopularProduct(x.p, x.Sold))
            .ToList();

        var soldIds = soldProducts.Select(x => x.ProductId).ToHashSet();
        var fallback = products
            .Where(p => !soldIds.Contains(p.Id))
            .OrderBy(p => p.Name)
            .Select(p => CreatePopularProduct(p, 0m));

        PopularProductsPanel.ItemsSource = soldProducts.Concat(fallback).Take(60).ToList();
    }

    private static PopularProduct CreatePopularProduct(Product product, decimal sold)
    {
        var unit = product.Units.SingleOrDefault(u => u.Id == product.BaseUnitId) ?? product.Units.SingleOrDefault(u => u.IsBaseUnit);
        return new PopularProduct(product.Id, unit?.Id ?? Guid.Empty, product.Sku, product.Name, unit?.SellingPrice ?? 0m, sold, unit?.Abbreviation ?? unit?.Name ?? "PCS");
    }

    private void KeypadTarget_GotKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
    {
        if (sender is TextBox textBox) _keypadTarget = textBox;
    }

    private void Keypad_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button button || button.Tag is not string value) return;
        var target = _keypadTarget ?? PaymentBox;
        if (value == "." && target.Text.Contains('.')) return;
        if (target == PaymentBox && target.Text == "0.00" && value != ".") target.Clear();
        if (target == PaymentBox && target.Text.Length >= 12) return;
        target.AppendText(value);
        target.CaretIndex = target.Text.Length;
        UpdateTouchChange();
    }

    private void KeypadBackspace_Click(object sender, RoutedEventArgs e)
    {
        var target = _keypadTarget ?? PaymentBox;
        if (target.Text.Length == 0) return;
        target.Text = target.Text[..^1];
        target.CaretIndex = target.Text.Length;
        UpdateTouchChange();
    }

    private void KeypadClear_Click(object sender, RoutedEventArgs e)
    {
        var target = _keypadTarget ?? PaymentBox;
        target.Clear();
        target.Focus();
        UpdateTouchChange();
    }

    private void PaymentBox_TextChanged(object sender, TextChangedEventArgs e) => UpdateTouchChange();

    private void UpdateTouchChange()
    {
        if (PaymentBox is null || TotalText is null || ChangeText is null) return;
        if (!decimal.TryParse(PaymentBox.Text, NumberStyles.Number, CultureInfo.CurrentCulture, out var received)) received = 0;
        if (!decimal.TryParse(TotalText.Text, NumberStyles.Number, CultureInfo.CurrentCulture, out var total)) total = 0;
        ChangeText.Text = Math.Max(0, received - total).ToString("N2", CultureInfo.CurrentCulture);
    }

    private async void CartDiscount_LostFocus(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { DataContext: CartItem item }) await RecalculateLineAsync(item);
    }

    private void PosWindow_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.F1) { e.Handled = true; MessageBox.Show(this, "F2 Hold\nF4 Invoice Discount\nF8 Void / Clear Cart\nF10 Charge\nCtrl+L Product Search", "POS Keyboard Shortcuts", MessageBoxButton.OK, MessageBoxImage.Information); return; }
        if (e.Key == Key.F2) { e.Handled = true; Hold_Click(this, new RoutedEventArgs()); return; }
        if (e.Key == Key.F4) { e.Handled = true; InvoiceDiscountBox.Focus(); InvoiceDiscountBox.SelectAll(); return; }
        if (e.Key == Key.F8) { e.Handled = true; ClearCart_Click(this, new RoutedEventArgs()); return; }
        if (e.Key == Key.F10) { e.Handled = true; Complete_Click(this, new RoutedEventArgs()); return; }
        if (e.Key == Key.L && Keyboard.Modifiers == ModifierKeys.Control) { e.Handled = true; BarcodeBox.Focus(); BarcodeBox.SelectAll(); }
    }

    private async void CheckProduct_Click(object sender, RoutedEventArgs e)
    {
        e.Handled = true;
        if (sender is not FrameworkElement { DataContext: PopularProduct item }) return;
        try
        {
            await using var db = await _dbFactory.CreateDbContextAsync();
            var product = await db.Products.AsNoTracking().Include(x => x.Units).SingleOrDefaultAsync(x => x.Id == item.ProductId);
            if (product is null) { Status("Product not found.", false); return; }
            var categoryName = product.CategoryId.HasValue ? await db.Categories.AsNoTracking().Where(x => x.Id == product.CategoryId.Value).Select(x => x.Name).SingleOrDefaultAsync() : null;
            var stocks = await db.Warehouses.AsNoTracking().Where(w => w.IsActive).Select(w => new ProductStockRow
            {
                Warehouse = $"{w.Code} — {w.Name}",
                Quantity = db.InventoryTransactions.Where(t => t.ProductId == product.Id && t.WarehouseId == w.Id).Sum(t => (decimal?)t.BaseQuantity) ?? 0m
            }).OrderBy(x => x.Warehouse).ToListAsync();

            var dialog = new Window { Owner = this, Title = $"Check Product — {product.Name}", Width = 720, Height = 560, WindowStartupLocation = WindowStartupLocation.CenterOwner, ResizeMode = ResizeMode.NoResize, Background = new SolidColorBrush(Color.FromRgb(246, 248, 251)) };
            var root = new Grid { Margin = new Thickness(24) };
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            var title = new TextBlock { Text = product.Name, FontSize = 24, FontWeight = FontWeights.Bold, Foreground = new SolidColorBrush(Color.FromRgb(15, 23, 42)) };
            Grid.SetRow(title, 0); root.Children.Add(title);
            var summary = new TextBlock { Margin = new Thickness(0, 6, 0, 18), Text = $"SKU: {product.Sku}\nCategory: {categoryName ?? "Uncategorized"}", FontSize = 13, Foreground = new SolidColorBrush(Color.FromRgb(71, 85, 105)) };
            Grid.SetRow(summary, 1); root.Children.Add(summary);
            var details = new StackPanel();
            foreach (var unit in product.Units.Where(x => x.IsActive).OrderByDescending(x => x.IsBaseUnit))
            {
                var card = new Border { Background = Brushes.White, BorderBrush = new SolidColorBrush(Color.FromRgb(226, 232, 240)), BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(5), Padding = new Thickness(14), Margin = new Thickness(0, 0, 0, 8) };
                var grid = new Grid();
                for (var i = 0; i < 5; i++) grid.ColumnDefinitions.Add(new ColumnDefinition { Width = i == 0 ? new GridLength(1, GridUnitType.Star) : GridLength.Auto });
                grid.Children.Add(new TextBlock { Text = $"{unit.Name} ({unit.Abbreviation}){(unit.IsBaseUnit ? " · BASE" : "")}", FontWeight = FontWeights.SemiBold });
                AddDialogMetric(grid, 1, "Cost", unit.PurchasePrice);
                AddDialogMetric(grid, 2, "Retail", unit.SellingPrice);
                AddDialogMetric(grid, 3, "Wholesale", unit.WholesalePrice);
                AddDialogMetric(grid, 4, "Wholesale+", unit.WholesaleWholesalePrice);
                card.Child = grid; details.Children.Add(card);
            }
            details.Children.Add(new TextBlock { Text = "Availability by Warehouse", FontSize = 16, FontWeight = FontWeights.Bold, Margin = new Thickness(0, 10, 0, 8) });
            details.Children.Add(new ListView { ItemsSource = stocks, Height = 180, BorderBrush = new SolidColorBrush(Color.FromRgb(226, 232, 240)), BorderThickness = new Thickness(1), ItemTemplate = BuildStockTemplate() });
            Grid.SetRow(details, 2); root.Children.Add(details);
            var close = new Button { Content = "Close", Width = 100, Height = 38, HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(0, 16, 0, 0), Background = new SolidColorBrush(Color.FromRgb(32, 166, 74)), Foreground = Brushes.White, BorderThickness = new Thickness(0), FontWeight = FontWeights.SemiBold };
            close.Click += (_, _) => dialog.Close(); Grid.SetRow(close, 3); root.Children.Add(close);
            dialog.Content = root; dialog.ShowDialog();
        }
        catch (Exception ex) { Status(ex.InnerException?.Message ?? ex.Message, false); }
    }

    private static void AddDialogMetric(Grid grid, int column, string label, decimal value)
    {
        var panel = new StackPanel { Margin = new Thickness(18, 0, 0, 0) };
        panel.Children.Add(new TextBlock { Text = label, FontSize = 9, Foreground = new SolidColorBrush(Color.FromRgb(100, 116, 139)) });
        panel.Children.Add(new TextBlock { Text = value.ToString("N2", CultureInfo.CurrentCulture), FontSize = 13, FontWeight = FontWeights.SemiBold });
        Grid.SetColumn(panel, column); grid.Children.Add(panel);
    }

    private static DataTemplate BuildStockTemplate()
    {
        var template = new DataTemplate();
        var panel = new FrameworkElementFactory(typeof(Grid));
        panel.SetValue(Grid.MarginProperty, new Thickness(10, 6, 10, 6));
        panel.AppendChild(BoundText(nameof(ProductStockRow.Warehouse), 0, false));
        panel.AppendChild(BoundText(nameof(ProductStockRow.Quantity), 1, true, "N3"));
        template.VisualTree = panel;
        return template;
    }

    private static FrameworkElementFactory BoundText(string property, int column, bool right, string? format = null)
    {
        var text = new FrameworkElementFactory(typeof(TextBlock));
        text.SetValue(Grid.ColumnProperty, column);
        text.SetValue(TextBlock.HorizontalAlignmentProperty, right ? HorizontalAlignment.Right : HorizontalAlignment.Left);
        text.SetBinding(TextBlock.TextProperty, new System.Windows.Data.Binding(property) { StringFormat = format });
        return text;
    }

    private async void ClosePos_Click(object sender, RoutedEventArgs e)
    {
        if (_cart.Count > 0 && MessageBox.Show(this, "The current cart contains items. Close the POS anyway?", "Close POS", MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes) return;
        var passwordBox = new PasswordBox { Height = 38, Margin = new Thickness(0, 8, 0, 0), FontSize = 16, Padding = new Thickness(10) };
        var dialog = new Window { Owner = this, Title = "Authorize Close POS", Width = 380, Height = 210, WindowStartupLocation = WindowStartupLocation.CenterOwner, ResizeMode = ResizeMode.NoResize, Background = Brushes.White };
        var panel = new StackPanel { Margin = new Thickness(24) };
        panel.Children.Add(new TextBlock { Text = "Enter your password to close the cashier screen.", FontSize = 14, FontWeight = FontWeights.SemiBold, TextWrapping = TextWrapping.Wrap });
        panel.Children.Add(passwordBox);
        var buttons = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(0, 16, 0, 0) };
        var cancel = new Button { Content = "Cancel", Width = 90, Height = 34, Margin = new Thickness(0, 0, 8, 0) };
        var confirm = new Button { Content = "Close POS", Width = 100, Height = 34, Background = new SolidColorBrush(Color.FromRgb(220, 38, 38)), Foreground = Brushes.White, BorderThickness = new Thickness(0) };
        cancel.Click += (_, _) => dialog.Close();
        confirm.Click += async (_, _) =>
        {
            var user = _session.CurrentUser;
            if (user is null) { MessageBox.Show(dialog, "Session expired.", "Close POS", MessageBoxButton.OK, MessageBoxImage.Error); return; }
            await using var db = await _dbFactory.CreateDbContextAsync();
            var stored = await db.Users.AsNoTracking().SingleOrDefaultAsync(x => x.Id == user.Id);
            var valid = stored is not null && new PasswordHasher().Verify(passwordBox.Password, stored.PasswordHash);
            if (!valid) { MessageBox.Show(dialog, "Invalid password.", "Close POS", MessageBoxButton.OK, MessageBoxImage.Error); passwordBox.Clear(); passwordBox.Focus(); return; }
            dialog.DialogResult = true; dialog.Close(); Close();
        };
        buttons.Children.Add(cancel); buttons.Children.Add(confirm); panel.Children.Add(buttons); dialog.Content = panel; dialog.ShowDialog();
    }

    private sealed class ProductStockRow
    {
        public string Warehouse { get; init; } = string.Empty;
        public decimal Quantity { get; init; }
    }
}
