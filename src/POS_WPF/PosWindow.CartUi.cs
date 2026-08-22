using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Markup;

namespace POS_WPF;

public partial class PosWindow
{
    private void InitializeCartUi()
    {
        ApplyCartItemTemplate();
        AlignTotalsValues();
    }

    private void ApplyCartItemTemplate()
    {
        var template = new DataTemplate();
        var root = new FrameworkElementFactory(typeof(Border));
        root.SetValue(Border.BorderBrushProperty, new SolidColorBrush(Color.FromRgb(238, 241, 245)));
        root.SetValue(Border.BorderThicknessProperty, new Thickness(0, 0, 0, 1));
        root.SetValue(Border.PaddingProperty, new Thickness(8, 7));

        var grid = new FrameworkElementFactory(typeof(Grid));
        grid.SetValue(FrameworkElement.VerticalAlignmentProperty, VerticalAlignment.Center);
        grid.AppendChild(CreateColumn(0, new GridLength(1, GridUnitType.Star), 120));
        grid.AppendChild(CreateColumn(1, new GridLength(58)));
        grid.AppendChild(CreateColumn(2, new GridLength(96)));
        grid.AppendChild(CreateColumn(3, new GridLength(58)));
        grid.AppendChild(CreateColumn(4, new GridLength(34)));

        var productPanel = new FrameworkElementFactory(typeof(StackPanel));
        productPanel.SetValue(StackPanel.OrientationProperty, Orientation.Horizontal);
        productPanel.SetValue(FrameworkElement.VerticalAlignmentProperty, VerticalAlignment.Center);
        productPanel.SetValue(FrameworkElement.MinWidthProperty, 0d);
        Grid.SetColumn(productPanel, 0);

        var product = new FrameworkElementFactory(typeof(TextBlock));
        product.SetBinding(TextBlock.TextProperty, new Binding(nameof(CartItem.ProductName)));
        product.SetValue(Control.FontSizeProperty, 11d);
        product.SetValue(Control.FontWeightProperty, FontWeights.SemiBold);
        product.SetValue(TextBlock.TextTrimmingProperty, TextTrimming.CharacterEllipsis);
        product.SetValue(FrameworkElement.VerticalAlignmentProperty, VerticalAlignment.Center);
        productPanel.AppendChild(product);

        var unit = new FrameworkElementFactory(typeof(TextBlock));
        unit.SetBinding(TextBlock.TextProperty, new Binding(nameof(CartItem.UnitName)));
        unit.SetValue(Control.FontSizeProperty, 8d);
        unit.SetValue(Control.ForegroundProperty, new SolidColorBrush(Color.FromRgb(148, 163, 184)));
        unit.SetValue(FrameworkElement.MarginProperty, new Thickness(6, 0, 0, 0));
        unit.SetValue(FrameworkElement.VerticalAlignmentProperty, VerticalAlignment.Center);
        productPanel.AppendChild(unit);
        grid.AppendChild(productPanel);

        var discount = new FrameworkElementFactory(typeof(TextBox));
        discount.SetValue(FrameworkElement.WidthProperty, 52d);
        discount.SetValue(FrameworkElement.HeightProperty, 24d);
        discount.SetValue(FrameworkElement.MarginProperty, new Thickness(3, 0));
        discount.SetValue(Control.FontSizeProperty, 9d);
        discount.SetValue(TextBox.TextAlignmentProperty, TextAlignment.Right);
        discount.SetValue(Control.VerticalContentAlignmentProperty, VerticalAlignment.Center);
        discount.SetValue(FrameworkElement.ToolTipProperty, "Discount");
        discount.SetBinding(TextBox.TextProperty, new Binding(nameof(CartItem.ManualDiscount))
        {
            Mode = BindingMode.TwoWay,
            UpdateSourceTrigger = UpdateSourceTrigger.LostFocus,
            StringFormat = "N2"
        });
        discount.AddHandler(TextBox.LostFocusEvent, new RoutedEventHandler(CartDiscount_LostFocus));
        Grid.SetColumn(discount, 1);
        grid.AppendChild(discount);

        var quantityPanel = new FrameworkElementFactory(typeof(StackPanel));
        quantityPanel.SetValue(StackPanel.OrientationProperty, Orientation.Horizontal);
        quantityPanel.SetValue(FrameworkElement.HorizontalAlignmentProperty, HorizontalAlignment.Center);
        quantityPanel.SetValue(FrameworkElement.VerticalAlignmentProperty, VerticalAlignment.Center);
        Grid.SetColumn(quantityPanel, 2);

        var decrease = CreateCircleButton("−", Color.FromRgb(220, 38, 38), 14);
        decrease.SetValue(FrameworkElement.ToolTipProperty, "Decrease quantity");
        decrease.AddHandler(Button.ClickEvent, new RoutedEventHandler(CartDecrease_Click));
        quantityPanel.AppendChild(decrease);

        var quantity = new FrameworkElementFactory(typeof(TextBlock));
        quantity.SetBinding(TextBlock.TextProperty, new Binding(nameof(CartItem.Quantity)) { StringFormat = "N0" });
        quantity.SetValue(FrameworkElement.WidthProperty, 28d);
        quantity.SetValue(TextBlock.TextAlignmentProperty, TextAlignment.Center);
        quantity.SetValue(FrameworkElement.VerticalAlignmentProperty, VerticalAlignment.Center);
        quantity.SetValue(Control.FontSizeProperty, 11d);
        quantity.SetValue(Control.FontWeightProperty, FontWeights.SemiBold);
        quantityPanel.AppendChild(quantity);

        var increase = CreateCircleButton("+", Color.FromRgb(22, 163, 74), 14);
        increase.SetValue(FrameworkElement.ToolTipProperty, "Increase quantity");
        increase.AddHandler(Button.ClickEvent, new RoutedEventHandler(CartIncrease_Click));
        quantityPanel.AppendChild(increase);
        grid.AppendChild(quantityPanel);

        var total = new FrameworkElementFactory(typeof(TextBlock));
        total.SetBinding(TextBlock.TextProperty, new Binding(nameof(CartItem.LineTotal)) { StringFormat = "N2" });
        total.SetValue(TextBlock.TextAlignmentProperty, TextAlignment.Right);
        total.SetValue(FrameworkElement.VerticalAlignmentProperty, VerticalAlignment.Center);
        total.SetValue(Control.FontSizeProperty, 11d);
        total.SetValue(Control.FontWeightProperty, FontWeights.Bold);
        total.SetValue(FrameworkElement.MarginProperty, new Thickness(2, 0));
        Grid.SetColumn(total, 3);
        grid.AppendChild(total);

        var delete = CreateCircleButton("🗑", Color.FromRgb(220, 38, 38), 14);
        delete.SetValue(Control.ForegroundProperty, new SolidColorBrush(Color.FromRgb(220, 38, 38)));
        delete.SetValue(Control.BackgroundProperty, new SolidColorBrush(Color.FromRgb(254, 242, 242)));
        delete.SetValue(Control.BorderBrushProperty, new SolidColorBrush(Color.FromRgb(254, 202, 202)));
        delete.SetValue(Control.BorderThicknessProperty, new Thickness(1));
        delete.SetValue(Control.FontSizeProperty, 12d);
        delete.SetValue(FrameworkElement.ToolTipProperty, "Remove product");
        delete.AddHandler(Button.ClickEvent, new RoutedEventHandler(CartDelete_Click));
        Grid.SetColumn(delete, 4);
        grid.AppendChild(delete);

        root.AppendChild(grid);
        template.VisualTree = root;
        template.Seal();
        CartGrid.ItemTemplate = template;
    }

    private static FrameworkElementFactory CreateColumn(int index, GridLength width, double? minWidth = null)
    {
        var definition = new FrameworkElementFactory(typeof(ColumnDefinition));
        definition.SetValue(ColumnDefinition.WidthProperty, width);
        if (minWidth.HasValue) definition.SetValue(ColumnDefinition.MinWidthProperty, minWidth.Value);
        return definition;
    }

    private static FrameworkElementFactory CreateCircleButton(string content, Color background, double radius)
    {
        var button = new FrameworkElementFactory(typeof(Button));
        button.SetValue(Button.ContentProperty, content);
        button.SetValue(Control.WidthProperty, 28d);
        button.SetValue(Control.HeightProperty, 28d);
        button.SetValue(Control.MinWidthProperty, 28d);
        button.SetValue(Control.MinHeightProperty, 28d);
        button.SetValue(Control.PaddingProperty, new Thickness(0));
        button.SetValue(Control.FontSizeProperty, 14d);
        button.SetValue(Control.FontWeightProperty, FontWeights.Bold);
        button.SetValue(Control.ForegroundProperty, Brushes.White);
        button.SetValue(Control.BackgroundProperty, new SolidColorBrush(background));
        button.SetValue(Control.BorderThicknessProperty, new Thickness(0));
        button.SetValue(Control.HorizontalContentAlignmentProperty, HorizontalAlignment.Center);
        button.SetValue(Control.VerticalContentAlignmentProperty, VerticalAlignment.Center);
        button.SetValue(FrameworkElement.MarginProperty, new Thickness(0));

        var template = new ControlTemplate(typeof(Button));
        var border = new FrameworkElementFactory(typeof(Border));
        border.SetValue(Border.CornerRadiusProperty, new CornerRadius(radius));
        border.SetValue(Border.BackgroundProperty, new TemplateBindingExtension(Control.BackgroundProperty));
        border.SetValue(Border.BorderBrushProperty, new TemplateBindingExtension(Control.BorderBrushProperty));
        border.SetValue(Border.BorderThicknessProperty, new TemplateBindingExtension(Control.BorderThicknessProperty));
        var presenter = new FrameworkElementFactory(typeof(ContentPresenter));
        presenter.SetValue(ContentPresenter.HorizontalAlignmentProperty, HorizontalAlignment.Center);
        presenter.SetValue(ContentPresenter.VerticalAlignmentProperty, VerticalAlignment.Center);
        presenter.SetValue(ContentPresenter.ContentProperty, new TemplateBindingExtension(ContentControl.ContentProperty));
        presenter.SetValue(ContentPresenter.ContentTemplateProperty, new TemplateBindingExtension(ContentControl.ContentTemplateProperty));
        border.AppendChild(presenter);
        template.VisualTree = border;
        button.SetValue(Control.TemplateProperty, template);
        return button;
    }

    private void CartIncrease_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not FrameworkElement element || element.DataContext is not CartItem item) return;
        item.Quantity += 1;
        _ = RecalculateLineAsync(item);
    }

    private void CartDecrease_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not FrameworkElement element || element.DataContext is not CartItem item) return;

        if (item.Quantity <= 1)
        {
            if (!ConfirmCartRemoval(item.ProductName, false)) return;
            _cart.Remove(item);
            RefreshTaxAndTotals();
            Status($"Removed {item.ProductName} from cart.", true);
            return;
        }

        item.Quantity -= 1;
        _ = RecalculateLineAsync(item);
    }

    private void CartDelete_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not FrameworkElement element || element.DataContext is not CartItem item) return;
        if (!ConfirmCartRemoval(item.ProductName, true)) return;
        _cart.Remove(item);
        RefreshTaxAndTotals();
        Status($"Removed {item.ProductName} from cart.", true);
    }

    private bool ConfirmCartRemoval(string productName, bool forceDelete)
    {
        var dialog = new Window
        {
            Owner = this,
            Width = 430,
            Height = 205,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            ResizeMode = ResizeMode.NoResize,
            WindowStyle = WindowStyle.ToolWindow,
            Title = "تأكيد حذف المنتج",
            FlowDirection = FlowDirection.RightToLeft,
            Background = Brushes.White,
            ShowInTaskbar = false
        };

        var root = new Grid { Margin = new Thickness(22) };
        root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        var message = forceDelete
            ? $"سيتم حذف المنتج «{productName}» من السلة مهما كانت الكمية. هل تريد المتابعة؟"
            : $"كمية المنتج «{productName}» وصلت إلى 1. إذا تابعت سيتم حذف المنتج من السلة. هل تريد المتابعة؟";

        var text = new TextBlock
        {
            Text = message,
            FontSize = 14,
            Foreground = new SolidColorBrush(Color.FromRgb(30, 41, 59)),
            TextWrapping = TextWrapping.Wrap,
            VerticalAlignment = VerticalAlignment.Center
        };
        Grid.SetRow(text, 0);
        root.Children.Add(text);

        var buttons = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Left,
            FlowDirection = FlowDirection.RightToLeft
        };

        var ok = new Button
        {
            Content = "موافق",
            Width = 95,
            Height = 36,
            Margin = new Thickness(0, 0, 8, 0),
            Background = new SolidColorBrush(Color.FromRgb(22, 163, 74)),
            Foreground = Brushes.White,
            BorderThickness = new Thickness(0),
            FontWeight = FontWeights.SemiBold
        };
        var cancel = new Button
        {
            Content = "إلغاء",
            Width = 95,
            Height = 36,
            Background = Brushes.White,
            Foreground = new SolidColorBrush(Color.FromRgb(51, 65, 85)),
            BorderBrush = new SolidColorBrush(Color.FromRgb(203, 213, 225)),
            BorderThickness = new Thickness(1),
            FontWeight = FontWeights.SemiBold
        };

        var confirmed = false;
        ok.Click += (_, _) => { confirmed = true; dialog.Close(); };
        cancel.Click += (_, _) => dialog.Close();
        buttons.Children.Add(ok);
        buttons.Children.Add(cancel);
        Grid.SetRow(buttons, 1);
        root.Children.Add(buttons);
        dialog.Content = root;
        dialog.ShowDialog();
        return confirmed;
    }

    private void AlignTotalsValues()
    {
        foreach (var text in new[] { SubtotalText, DiscountText, TaxText })
        {
            text.HorizontalAlignment = HorizontalAlignment.Right;
            text.TextAlignment = TextAlignment.Right;
            text.Width = 88;
            text.VerticalAlignment = VerticalAlignment.Center;
        }
    }
}
