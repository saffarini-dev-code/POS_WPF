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

    private void ApplyCashierReferenceLayout()
    {
        if (_paymentLayoutApplied) return;
        if (CardButton is null || CashButton is null || MobileButton is null || PaymentBox is null || ChangeText is null || StatusText is null) return;

        _paymentLayoutApplied = true;

        if (Content is Grid root && root.RowDefinitions.Count >= 3)
        {
            root.RowDefinitions[0].Height = new GridLength(44);
            root.RowDefinitions[1].Height = new GridLength(1, GridUnitType.Star);
            root.RowDefinitions[2].Height = new GridLength(28);
        }

        var paymentGrid = FindAncestor<Grid>(CardButton, grid => Grid.GetRow(grid) == 3);
        if (paymentGrid is null) return;

        if (paymentGrid.Parent is Border { Parent: Grid rightPanelGrid })
        {
            if (rightPanelGrid.RowDefinitions.Count >= 4)
            {
                rightPanelGrid.RowDefinitions[0].Height = new GridLength(62);
                rightPanelGrid.RowDefinitions[1].Height = new GridLength(1, GridUnitType.Star);
                rightPanelGrid.RowDefinitions[2].Height = new GridLength(106);
                rightPanelGrid.RowDefinitions[3].Height = new GridLength(178);
            }
        }

        var workspaceGrid = FindAncestor<Grid>(paymentGrid, grid => grid.ColumnDefinitions.Count == 2);
        if (workspaceGrid is not null && workspaceGrid.ColumnDefinitions.Count == 2)
        {
            workspaceGrid.ColumnDefinitions[0].Width = new GridLength(3, GridUnitType.Star);
            workspaceGrid.ColumnDefinitions[1].Width = new GridLength(1, GridUnitType.Star);
        }

        DetachElement(CardButton);
        DetachElement(CashButton);
        DetachElement(MobileButton);
        DetachElement(PaymentBox);
        DetachElement(ChangeText);
        DetachElement(StatusText);

        paymentGrid.Children.Clear();
        paymentGrid.RowDefinitions.Clear();
        paymentGrid.ColumnDefinitions.Clear();
        paymentGrid.Margin = new Thickness(0);
        paymentGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        var paymentContent = new Grid { Margin = new Thickness(14, 7, 14, 5) };
        paymentContent.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        paymentContent.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        paymentContent.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        paymentContent.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        paymentContent.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        paymentContent.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        paymentContent.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        var paymentTitle = new TextBlock
        {
            Text = "PAYMENT METHOD",
            FontSize = 9,
            FontWeight = FontWeights.SemiBold,
            Foreground = Brush("#94A3B8"),
            Margin = new Thickness(0, 0, 0, 5)
        };
        Grid.SetRow(paymentTitle, 0);
        paymentContent.Children.Add(paymentTitle);

        ConfigurePaymentButton(CardButton, "▣ Card", new Thickness(0, 0, 4, 0));
        ConfigurePaymentButton(CashButton, "▤ Cash", new Thickness(2, 0, 2, 0));
        ConfigurePaymentButton(MobileButton, "▥ Mobile", new Thickness(4, 0, 0, 0));

        var methods = new Grid();
        methods.ColumnDefinitions.Add(new ColumnDefinition());
        methods.ColumnDefinitions.Add(new ColumnDefinition());
        methods.ColumnDefinitions.Add(new ColumnDefinition());
        Grid.SetColumn(CardButton, 0);
        Grid.SetColumn(CashButton, 1);
        Grid.SetColumn(MobileButton, 2);
        methods.Children.Add(CardButton);
        methods.Children.Add(CashButton);
        methods.Children.Add(MobileButton);
        Grid.SetRow(methods, 1);
        paymentContent.Children.Add(methods);

        var amountLabel = new TextBlock
        {
            Text = "AMOUNT RECEIVED",
            FontSize = 9,
            Foreground = Brush("#94A3B8"),
            Margin = new Thickness(0, 5, 0, 3)
        };
        Grid.SetRow(amountLabel, 2);
        paymentContent.Children.Add(amountLabel);

        PaymentBox.Height = 36;
        PaymentBox.FontSize = 18;
        PaymentBox.FontWeight = FontWeights.Bold;
        PaymentBox.HorizontalContentAlignment = HorizontalAlignment.Right;
        PaymentBox.VerticalContentAlignment = VerticalAlignment.Center;
        PaymentBox.Margin = new Thickness(0);
        Grid.SetRow(PaymentBox, 3);
        paymentContent.Children.Add(PaymentBox);

        var changePanel = new Grid { Margin = new Thickness(0, 4, 0, 0) };
        var changeLabel = new TextBlock
        {
            Text = "Change",
            FontSize = 9,
            Foreground = Brush("#94A3B8"),
            VerticalAlignment = VerticalAlignment.Center
        };
        ChangeText.FontSize = 13;
        ChangeText.FontWeight = FontWeights.Bold;
        ChangeText.Foreground = Brush("#16A34A");
        ChangeText.HorizontalAlignment = HorizontalAlignment.Right;
        ChangeText.VerticalAlignment = VerticalAlignment.Center;
        changePanel.Children.Add(changeLabel);
        changePanel.Children.Add(ChangeText);
        Grid.SetRow(changePanel, 4);
        paymentContent.Children.Add(changePanel);

        var charge = CreateActionButton("CHARGE", 38, Brush("#20A64A"), Brushes.White);
        charge.Click += Complete_Click;
        charge.Margin = new Thickness(0, 5, 0, 0);
        Grid.SetRow(charge, 5);
        paymentContent.Children.Add(charge);

        var holdRecall = new Grid { Margin = new Thickness(0, 5, 0, 0) };
        holdRecall.ColumnDefinitions.Add(new ColumnDefinition());
        holdRecall.ColumnDefinitions.Add(new ColumnDefinition());

        var hold = CreateActionButton("Hold", 32, Brushes.White, Brush("#64748B"));
        hold.Margin = new Thickness(0, 0, 4, 0);
        hold.Click += Hold_Click;
        Grid.SetColumn(hold, 0);
        holdRecall.Children.Add(hold);

        var recall = CreateActionButton("Recall", 32, Brushes.White, Brush("#64748B"));
        recall.Margin = new Thickness(4, 0, 0, 0);
        recall.Click += Recall_Click;
        Grid.SetColumn(recall, 1);
        holdRecall.Children.Add(recall);

        Grid.SetRow(holdRecall, 6);
        paymentContent.Children.Add(holdRecall);

        StatusText.FontSize = 9;
        StatusText.Margin = new Thickness(0, 2, 0, 0);
        StatusText.HorizontalAlignment = HorizontalAlignment.Left;
        Grid.SetRow(StatusText, 6);
        StatusText.Visibility = Visibility.Collapsed;
        paymentContent.Children.Add(StatusText);

        paymentGrid.Children.Add(paymentContent);

        HookResponsiveCashierSizing();
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
            if (button.ClickMode != ClickMode.Hover) continue;
            button.Width = width;
            button.Height = 86;
            button.Margin = new Thickness(5);
            button.Padding = new Thickness(12);
        }
    }

    private void ResizeCategoryTabs()
    {
        if (CategoryTabsPanel is null) return;
        foreach (var button in CategoryTabsPanel.Children.OfType<Button>())
        {
            button.Height = 28;
            button.Margin = new Thickness(0, 0, 6, 0);
            button.Padding = new Thickness(12, 0, 12, 0);
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
        if (!decimal.TryParse(ReceiptTaxText.Text, NumberStyles.Number, CultureInfo.CurrentCulture, out var tax)) tax = 0m;
        var visible = Math.Abs(tax) > 0.000001m;
        if (TaxText is not null) TaxText.Visibility = visible ? Visibility.Visible : Visibility.Collapsed;

        foreach (var textBlock in FindVisualChildren<TextBlock>(this))
        {
            if (string.Equals(textBlock.Text?.Trim(), "Tax", StringComparison.OrdinalIgnoreCase))
                textBlock.Visibility = visible ? Visibility.Visible : Visibility.Collapsed;
        }
    }

    private Button CreateActionButton(string text, double height, Brush background, Brush foreground)
    {
        return new Button
        {
            Content = text,
            Height = height,
            Background = background,
            Foreground = foreground,
            BorderBrush = background == Brushes.White ? Brush("#D7DEE8") : background,
            Style = (Style)FindResource("BaseButton"),
            HorizontalContentAlignment = HorizontalAlignment.Center,
            VerticalContentAlignment = VerticalAlignment.Center,
            FontSize = 12
        };
    }

    private static void ConfigurePaymentButton(Button button, string content, Thickness margin)
    {
        button.Content = content;
        button.Width = double.NaN;
        button.Height = 34;
        button.Margin = margin;
        button.HorizontalContentAlignment = HorizontalAlignment.Center;
        button.VerticalContentAlignment = VerticalAlignment.Center;
        button.FontSize = 11;
    }

    private static void DetachElement(FrameworkElement element)
    {
        switch (element.Parent)
        {
            case Panel panel:
                panel.Children.Remove(element);
                break;
            case ContentControl contentControl when ReferenceEquals(contentControl.Content, element):
                contentControl.Content = null;
                break;
            case Decorator decorator when ReferenceEquals(decorator.Child, element):
                decorator.Child = null;
                break;
        }
    }

    private static Brush Brush(string hex) => new SolidColorBrush((Color)ColorConverter.ConvertFromString(hex));

    private static T? FindAncestor<T>(DependencyObject start, Func<T, bool>? predicate = null) where T : DependencyObject
    {
        var current = VisualTreeHelper.GetParent(start);
        while (current is not null)
        {
            if (current is T typed && (predicate is null || predicate(typed))) return typed;
            current = VisualTreeHelper.GetParent(current);
        }
        return null;
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