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

        var hasTax = decimal.TryParse(
            ReceiptTaxText.Text,
            System.Globalization.NumberStyles.Number,
            System.Globalization.CultureInfo.CurrentCulture,
            out var tax) && Math.Abs(tax) > 0.000001m;

        TaxLabel.Visibility = hasTax ? Visibility.Visible : Visibility.Collapsed;
        TaxText.Visibility = hasTax ? Visibility.Visible : Visibility.Collapsed;
    }

    private static bool RegisterUiPolishHandlers()
    {
        EventManager.RegisterClassHandler(
            typeof(PosWindow),
            FrameworkElement.LoadedEvent,
            new RoutedEventHandler(OnPosWindowLoadedForUiPolish));

        EventManager.RegisterClassHandler(
            typeof(Window),
            FrameworkElement.LoadedEvent,
            new RoutedEventHandler(OnWindowLoadedForDialogChrome));

        return true;
    }

    private static void OnPosWindowLoadedForUiPolish(object sender, RoutedEventArgs e)
    {
        if (sender is not PosWindow window || window.CartGrid is null) return;

        // WPF's default ListViewItem template binds HorizontalContentAlignment to its
        // ancestor ItemsControl. During the initial visual-tree construction that
        // binding can legitimately resolve to null and produce a noisy runtime error.
        // Use an explicit container template for the POS cart instead.
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

        border.AppendChild(presenter);
        template.VisualTree = border;
        style.Setters.Add(new Setter(Control.TemplateProperty, template));
        window.CartGrid.ItemContainerStyle = style;
    }

    private static void OnWindowLoadedForDialogChrome(object sender, RoutedEventArgs e)
    {
        if (sender is not Window dialog || dialog.Title != "Authorize Close POS" || Equals(dialog.Tag, "CompactDialogStyled"))
            return;

        dialog.Tag = "CompactDialogStyled";
        dialog.Width = 384;
        dialog.Height = 300;
        dialog.MinWidth = 384;
        dialog.MinHeight = 300;
        dialog.MaxWidth = 384;
        dialog.MaxHeight = 300;
        dialog.WindowStyle = WindowStyle.None;
        dialog.ResizeMode = ResizeMode.NoResize;
        dialog.AllowsTransparency = true;
        dialog.Background = Brushes.Transparent;
        dialog.ShowInTaskbar = false;
        dialog.WindowStartupLocation = WindowStartupLocation.CenterOwner;

        if (dialog.Content is not StackPanel panel)
            return;

        panel.Margin = new Thickness(0);
        panel.VerticalAlignment = VerticalAlignment.Stretch;

        foreach (var child in panel.Children)
        {
            if (child is TextBlock heading)
            {
                heading.FontSize = 13;
                heading.Foreground = new SolidColorBrush(Color.FromRgb(100, 116, 139));
                heading.FontWeight = FontWeights.SemiBold;
            }
            else if (child is PasswordBox password)
            {
                password.Height = 42;
                password.Margin = new Thickness(0, 14, 0, 0);
                password.FontSize = 16;
                password.Padding = new Thickness(10);
            }
            else if (child is StackPanel buttons)
            {
                buttons.HorizontalAlignment = HorizontalAlignment.Stretch;
                buttons.Margin = new Thickness(0, 22, 0, 0);
                buttons.FlowDirection = FlowDirection.LeftToRight;
                foreach (var button in buttons.Children.OfType<Button>())
                {
                    button.Height = 42;
                    button.Margin = new Thickness(0, 0, 8, 0);
                    button.FontSize = 13;
                    button.FontWeight = FontWeights.SemiBold;
                }

                if (buttons.Children.OfType<Button>().LastOrDefault() is Button confirm)
                {
                    confirm.Background = new SolidColorBrush(Color.FromRgb(32, 166, 74));
                    confirm.Foreground = Brushes.White;
                    confirm.BorderBrush = new SolidColorBrush(Color.FromRgb(32, 166, 74));
                }
            }
        }

        dialog.Content = new Border
        {
            Width = 384,
            Height = 300,
            Background = Brushes.White,
            CornerRadius = new CornerRadius(8),
            BorderBrush = new SolidColorBrush(Color.FromRgb(226, 232, 240)),
            BorderThickness = new Thickness(1),
            Padding = new Thickness(24),
            Child = panel
        };
    }
}
