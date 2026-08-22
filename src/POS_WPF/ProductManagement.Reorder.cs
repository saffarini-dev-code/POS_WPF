using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using Microsoft.EntityFrameworkCore;
using POS_WPF.Data;

namespace POS_WPF;

public partial class ProductManagementWindow
{
    private static readonly bool _reorderUiRegistered = RegisterReorderUiHandlers();
    private TextBox? _reorderLevelBox;

    private static bool RegisterReorderUiHandlers()
    {
        EventManager.RegisterClassHandler(typeof(ProductManagementWindow), FrameworkElement.LoadedEvent, new RoutedEventHandler(OnProductManagementLoaded));
        EventManager.RegisterClassHandler(typeof(ProductManagementWindow), ButtonBase.ClickEvent, new RoutedEventHandler(OnProductManagementButtonClick));
        return true;
    }

    private static void OnProductManagementLoaded(object sender, RoutedEventArgs e)
    {
        if (sender is not ProductManagementWindow window || window._reorderLevelBox is not null) return;
        window.AddReorderLevelField();
        window.ProductsGrid.SelectionChanged += async (_, _) => await window.LoadSelectedReorderLevelAsync();
        window.NewProductReorderLevel();
    }

    private void AddReorderLevelField()
    {
        var parent = OpeningStockBox.Parent as Grid;
        if (parent is null) return;
        parent.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        var label = new TextBlock { Text = "Reorder Level", Margin = new Thickness(0, 8, 10, 5) };
        var box = new TextBox { Text = "0", Margin = new Thickness(0, 6, 10, 12), ToolTip = "Base-unit quantity at or below which stock is considered low." };
        var stack = new StackPanel { Margin = new Thickness(0, 0, 10, 0) };
        stack.Children.Add(label);
        stack.Children.Add(box);
        Grid.SetRow(stack, parent.RowDefinitions.Count - 1);
        Grid.SetColumn(stack, 0);
        parent.Children.Add(stack);
        _reorderLevelBox = box;
    }

    private void NewProductReorderLevel(){if(_reorderLevelBox is not null)_reorderLevelBox.Text="0";}

    private async Task LoadSelectedReorderLevelAsync()
    {
        if (_reorderLevelBox is null) return;
        await Dispatcher.InvokeAsync(async () =>
        {
            if (ProductsGrid.SelectedItem is not object selected) { _reorderLevelBox.Text = "0"; return; }
            var idProperty = selected.GetType().GetProperty("Id");
            if (idProperty?.GetValue(selected) is not Guid id) return;
            await using var db = await _dbFactory.CreateDbContextAsync();
            var level = await db.Products.AsNoTracking().Where(x => x.Id == id).Select(x => x.ReorderLevel).SingleOrDefaultAsync();
            _reorderLevelBox.Text = level.ToString("0.###", CultureInfo.CurrentCulture);
        });
    }

    private static void OnProductManagementButtonClick(object sender, RoutedEventArgs e)
    {
        if (sender is not ProductManagementWindow window || e.OriginalSource is not Button button) return;
        var caption = button.Content?.ToString() ?? string.Empty;
        if (caption.StartsWith("＋  New Product", StringComparison.Ordinal)) { window.NewProductReorderLevel(); return; }
        if (!string.Equals(caption, "✓  Save Product", StringComparison.Ordinal)) return;
        var sku = window.SkuBox.Text.Trim();
        var levelText = window._reorderLevelBox?.Text?.Trim() ?? "0";
        if (string.IsNullOrWhiteSpace(sku) || !decimal.TryParse(levelText, NumberStyles.Number, CultureInfo.CurrentCulture, out var level) || level < 0) return;
        window.Dispatcher.BeginInvoke(async () => await window.PersistReorderLevelAsync(sku, level), System.Windows.Threading.DispatcherPriority.Background);
    }

    private async Task PersistReorderLevelAsync(string sku, decimal level)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var product = await db.Products.SingleOrDefaultAsync(x => x.Sku == sku);
        if (product is null) return;
        product.ReorderLevel = level;
        await db.SaveChangesAsync();
        NewProductReorderLevel();
    }
}
