using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;

namespace POS_WPF;

public partial class PosWindow
{
    private bool _paymentLayoutApplied;
    private bool _taxVisibilityHooked;
    private bool _cashierResponsiveLayoutHooked;

    static PosWindow()
    {
        EventManager.RegisterClassHandler(
            typeof(PosWindow),
            FrameworkElement.LoadedEvent,
            new RoutedEventHandler(OnPosWindowLoadedForLayout));
    }

    private static void OnPosWindowLoadedForLayout(object sender, RoutedEventArgs e)
    {
        if (sender is not PosWindow window) return;
        window.ApplyCashierReferenceLayout();
        window.HookTaxVisibility();
    }

    /// <summary>
    /// The XAML is the single source of truth for the cashier layout.
    /// Never detach or re-parent named controls during Loaded; doing that can
    /// cause WPF InvalidOperationException when a control is already attached.
    /// </summary>
    private void ApplyCashierReferenceLayout()
    {
        if (_paymentLayoutApplied) return;
        _paymentLayoutApplied = true;

        if (Content is not Grid root || root.RowDefinitions.Count < 3) return;

        root.RowDefinitions[0].Height = new GridLength(41);
        root.RowDefinitions[1].Height = new GridLength(1, GridUnitType.Star);
        root.RowDefinitions[2].Height = new GridLength(41);

        var workspace = root.Children.OfType<Grid>().FirstOrDefault(x => Grid.GetRow(x) == 1);
        if (workspace is not null && workspace.ColumnDefinitions.Count == 2)
        {
            workspace.ColumnDefinitions[0].Width = new GridLength(4, GridUnitType.Star);
            workspace.ColumnDefinitions[1].Width = new GridLength(1, GridUnitType.Star);
        }

        var rightPanel = workspace?.Children
            .OfType<Border>()
            .FirstOrDefault(x => Grid.GetColumn(x) == 1)
            ?.Child as Grid;

        if (rightPanel is not null && rightPanel.RowDefinitions.Count >= 4)
        {
            rightPanel.RowDefinitions[0].Height = new GridLength(58);
            rightPanel.RowDefinitions[1].Height = new GridLength(1, GridUnitType.Star);
            rightPanel.RowDefinitions[2].Height = new GridLength(120);
            // Enough vertical room for the 4x3 touch keypad plus Hold/Recall.
            rightPanel.RowDefinitions[3].Height = new GridLength(190);
            ResizeTouchKeypad(rightPanel);
        }

        HookResponsiveCashierSizing();
    }

    private static void ResizeTouchKeypad(DependencyObject rightPanel)
    {
        foreach (var keypad in FindVisualChildren<UniformGrid>(rightPanel))
        {
            foreach (var button in keypad.Children.OfType<Button>())
            {
                button.Height = 40;
                button.Margin = new Thickness(2);
                button.Padding = new Thickness(2);
                button.FontSize = 17;
            }
        }
    }

    private void HookResponsiveCashierSizing()
    {
        if (_cashierResponsiveLayoutHooked || PopularProductsPanel is null) return;
        _cashierResponsiveLayoutHooked = true;
        PopularProductsPanel.SizeChanged += (_, _) => ResizeProductCards();
        CategoryTabsPanel.SizeChanged += (_, _) => ResizeCategoryTabs();
        Dispatcher.BeginInvoke(DispatcherPriority.Loaded, new Action(() =>
        {
            ResizeProductCards();
            ResizeCategoryTabs();
        }));
    }

    private void ResizeProductCards()
    {
        if (PopularProductsPanel is null || PopularProductsPanel.ActualWidth <= 0) return;
        var width = Math.Max(180, Math.Floor((PopularProductsPanel.ActualWidth - 40) / 5.0));
        foreach (var button in FindVisualChildren<Button>(PopularProductsPanel))
        {
            if (button.Tag is not null) continue;
            button.Width = width;
            button.Height = 84;
            button.Margin = new Thickness(0, 0, 8, 10);
            button.Padding = new Thickness(12);
        }
    }

    private void ResizeCategoryTabs()
    {
        if (CategoryTabsPanel is null) return;
        foreach (var button in CategoryTabsPanel.Children.OfType<Button>())
        {
            button.Height = 28;
            button.Margin = new Thickness(0, 0, 7, 0);
            button.Padding = new Thickness(14, 0, 14, 0);
            button.FontSize = 11;
        }
    }

    private void HookTaxVisibility()
    {
        if (_taxVisibilityHooked || ReceiptTaxText is null) return;
        _taxVisibilityHooked = true;
        ReceiptTaxText.TextChanged += (_, _) => UpdateTaxVisibility();
        UpdateTaxVisibility();
    }

    private void UpdateTaxVisibility()
    {
        if (ReceiptTaxText is null) return;
        if (!decimal.TryParse(ReceiptTaxText.Text, NumberStyles.Number, CultureInfo.CurrentCulture, out var tax))
            tax = 0m;

        var visible = Math.Abs(tax) > 0.000001m;
        if (TaxLabel is not null) TaxLabel.Visibility = visible ? Visibility.Visible : Visibility.Collapsed;
        if (TaxText is not null) TaxText.Visibility = visible ? Visibility.Visible : Visibility.Collapsed;
    }

    private static IEnumerable<T> FindVisualChildren<T>(DependencyObject root) where T : DependencyObject
    {
        if (root is null) yield break;
        for (var i = 0; i < VisualTreeHelper.GetChildrenCount(root); i++)
        {
            var child = VisualTreeHelper.GetChild(root, i);
            if (child is T match) yield return match;
            foreach (var descendant in FindVisualChildren<T>(child)) yield return descendant;
        }
    }
}
