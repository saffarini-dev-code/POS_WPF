using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Data;

namespace POS_WPF;

public partial class PosWindow
{
    private bool _taxWatcherAttached;
    private static readonly bool _uiPolishRegistered = RegisterUiPolishHandlers();

    private void AttachTaxVisibilityWatcher()
    {
        if (_taxWatcherAttached || ReceiptTaxText is null) return;
        _taxWatcherAttached = true;
        var descriptor = DependencyPropertyDescriptor.FromProperty(TextBlock.TextProperty, typeof(TextBlock));
        descriptor?.AddValueChanged(ReceiptTaxText, (_, _) => UpdateTaxVisibility());
        UpdateTaxVisibility();
    }

    private void UpdateTaxVisibility()
    {
        if (ReceiptTaxText is null) return;
        var hasTax = decimal.TryParse(ReceiptTaxText.Text, System.Globalization.NumberStyles.Number, System.Globalization.CultureInfo.CurrentCulture, out var tax) && Math.Abs(tax) > 0.000001m;
        TaxLabel.Visibility = hasTax ? Visibility.Visible : Visibility.Collapsed;
        TaxText.Visibility = hasTax ? Visibility.Visible : Visibility.Collapsed;
    }

    private static bool RegisterUiPolishHandlers()
    {
        EventManager.RegisterClassHandler(typeof(PosWindow), FrameworkElement.LoadedEvent, new RoutedEventHandler(OnPosWindowLoadedForUiPolish));
        return true;
    }

    private static void OnPosWindowLoadedForUiPolish(object sender, RoutedEventArgs e)
    {
        if (sender is not PosWindow window || window.CartGrid is null) return;

        var style = new Style(typeof(ListViewItem));
        style.Setters.Add(new Setter(Control.HorizontalContentAlignmentProperty, HorizontalAlignment.Stretch));
        style.Setters.Add(new Setter(Control.VerticalContentAlignmentProperty, VerticalAlignment.Center));
        style.Setters.Add(new Setter(Control.PaddingProperty, new Thickness(0)));
        style.Setters.Add(new Setter(Control.MarginProperty, new Thickness(0)));
        var template = new ControlTemplate(typeof(ListViewItem));
        var border = new FrameworkElementFactory(typeof(Border));
        border.SetBinding(Border.BackgroundProperty, new Binding(nameof(Control.Background)) { RelativeSource = new RelativeSource(RelativeSourceMode.TemplatedParent) });
        border.SetBinding(Border.BorderBrushProperty, new Binding(nameof(Control.BorderBrush)) { RelativeSource = new RelativeSource(RelativeSourceMode.TemplatedParent) });
        border.SetBinding(Border.BorderThicknessProperty, new Binding(nameof(Control.BorderThickness)) { RelativeSource = new RelativeSource(RelativeSourceMode.TemplatedParent) });
        border.SetBinding(Border.PaddingProperty, new Binding(nameof(Control.Padding)) { RelativeSource = new RelativeSource(RelativeSourceMode.TemplatedParent) });
        var presenter = new FrameworkElementFactory(typeof(ContentPresenter));
        presenter.SetBinding(ContentPresenter.ContentProperty, new Binding(nameof(ContentControl.Content)) { RelativeSource = new RelativeSource(RelativeSourceMode.TemplatedParent) });
        presenter.SetBinding(ContentPresenter.ContentTemplateProperty, new Binding(nameof(ContentControl.ContentTemplate)) { RelativeSource = new RelativeSource(RelativeSourceMode.TemplatedParent) });
        presenter.SetBinding(ContentPresenter.ContentStringFormatProperty, new Binding(nameof(ContentControl.ContentStringFormat)) { RelativeSource = new RelativeSource(RelativeSourceMode.TemplatedParent) });
        presenter.SetBinding(ContentPresenter.HorizontalAlignmentProperty, new Binding(nameof(Control.HorizontalContentAlignment)) { RelativeSource = new RelativeSource(RelativeSourceMode.TemplatedParent) });
        presenter.SetBinding(ContentPresenter.VerticalAlignmentProperty, new Binding(nameof(Control.VerticalContentAlignment)) { RelativeSource = new RelativeSource(RelativeSourceMode.TemplatedParent) });
        border.AppendChild(presenter); template.VisualTree = border; style.Setters.Add(new Setter(Control.TemplateProperty, template)); window.CartGrid.ItemContainerStyle = style;
        ApplyPaymentAndTotalsPolish(window);
    }

    private static void ApplyPaymentAndTotalsPolish(PosWindow window)
    {
        foreach (var button in new[] { window.CashButton, window.CardButton, window.MobileButton })
        {
            button.Height = 38;
            button.FontSize = 13;
            button.FontWeight = FontWeights.SemiBold;
        }
        window.CardButton.Content = "Credit Card";
        window.MobileButton.Content = "Mobile Pay";

        foreach (var value in new[] { window.SubtotalText, window.DiscountText, window.TaxText })
        {
            value.FontSize = 13;
            value.FontWeight = FontWeights.SemiBold;
            value.HorizontalAlignment = HorizontalAlignment.Right;
            value.TextAlignment = TextAlignment.Right;
            value.Width = 96;
            value.VerticalAlignment = VerticalAlignment.Center;
        }

        window.TotalText.FontSize = 20;
        window.ChangeText.FontSize = 13;

        foreach (var text in FindVisualChildren<TextBlock>(window))
        {
            if (text.Text == "Subtotal" || text.Text == "Discount" || text.Text.StartsWith("Tax", StringComparison.Ordinal))
            {
                text.FontSize = 13;
                text.FontWeight = FontWeights.SemiBold;
            }
        }
    }

    private static IEnumerable<T> FindVisualChildren<T>(DependencyObject root) where T : DependencyObject
    {
        if (root is null) yield break;
        for (var i = 0; i < VisualTreeHelper.GetChildrenCount(root); i++)
        {
            var child = VisualTreeHelper.GetChild(root, i);
            if (child is T typed) yield return typed;
            foreach (var descendant in FindVisualChildren<T>(child)) yield return descendant;
        }
    }
}
