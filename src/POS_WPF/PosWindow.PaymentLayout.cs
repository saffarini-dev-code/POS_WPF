using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace POS_WPF;

public partial class PosWindow
{
    private bool _paymentLayoutApplied;
    private bool _taxVisibilityHooked;

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
        window.ApplyPaymentLayout();
        window.HookTaxVisibility();
    }

    private void ApplyPaymentLayout()
    {
        if (_paymentLayoutApplied) return;
        if (CardButton is null || CashButton is null || MobileButton is null || PaymentBox is null || ChangeText is null || StatusText is null) return;

        var paymentGrid = FindAncestor<Grid>(CardButton, grid => Grid.GetRow(grid) == 3);
        if (paymentGrid is null) return;

        _paymentLayoutApplied = true;

        // Detach the controls we keep before removing their old StackPanel parent.
        if (CardButton.Parent is Panel oldPanel)
            oldPanel.Children.Clear();

        paymentGrid.Children.Clear();
        paymentGrid.RowDefinitions.Clear();
        paymentGrid.ColumnDefinitions.Clear();
        paymentGrid.Margin = new Thickness(12, 10, 12, 10);

        paymentGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        paymentGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(188) });

        var paymentBorder = new Border
        {
            Background = Brushes.White,
            BorderBrush = new SolidColorBrush(Color.FromRgb(226, 232, 240)),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(5),
            Padding = new Thickness(10),
            Margin = new Thickness(0, 0, 8, 0)
        };
        Grid.SetColumn(paymentBorder, 0);

        var paymentContent = new Grid();
        paymentContent.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        paymentContent.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        paymentContent.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        paymentContent.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        paymentContent.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        var paymentTitle = new TextBlock
        {
            Text = "PAYMENT",
            FontSize = 10,
            FontWeight = FontWeights.SemiBold,
            Foreground = new SolidColorBrush(Color.FromRgb(148, 163, 184)),
            Margin = new Thickness(0, 0, 0, 7)
        };
        Grid.SetRow(paymentTitle, 0);
        paymentContent.Children.Add(paymentTitle);

        ConfigurePaymentButton(CardButton, "▣  Card", new Thickness(0, 0, 5, 0));
        ConfigurePaymentButton(CashButton, "▤  Cash", new Thickness(0, 0, 5, 0));
        ConfigurePaymentButton(MobileButton, "▥  Mobile", new Thickness(0));
        var methods = new UniformGrid { Columns = 3, Margin = new Thickness(0, 0, 0, 8) };
        methods.Children.Add(CardButton);
        methods.Children.Add(CashButton);
        methods.Children.Add(MobileButton);
        Grid.SetRow(methods, 1);
        paymentContent.Children.Add(methods);

        var amountLabel = new TextBlock
        {
            Text = "AMOUNT RECEIVED",
            FontSize = 9,
            Foreground = new SolidColorBrush(Color.FromRgb(148, 163, 184)),
            Margin = new Thickness(0, 0, 0, 3)
        };
        Grid.SetRow(amountLabel, 2);
        paymentContent.Children.Add(amountLabel);

        var amountArea = new Grid();
        amountArea.RowDefinitions.Add(new RowDefinition { Height = new GridLength(44) });
        amountArea.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        amountArea.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        PaymentBox.Height = 44;
        PaymentBox.FontSize = 20;
        PaymentBox.FontWeight = FontWeights.Bold;
        PaymentBox.HorizontalContentAlignment = HorizontalAlignment.Right;
        PaymentBox.VerticalContentAlignment = VerticalAlignment.Center;
        Grid.SetRow(PaymentBox, 0);
        amountArea.Children.Add(PaymentBox);

        var changePanel = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 6, 0, 0) };
        var changeLabel = new TextBlock
        {
            Text = "Change",
            FontSize = 10,
            Foreground = new SolidColorBrush(Color.FromRgb(148, 163, 184)),
            VerticalAlignment = VerticalAlignment.Center
        };
        changePanel.Children.Add(changeLabel);
        ChangeText.FontSize = 14;
        ChangeText.FontWeight = FontWeights.Bold;
        ChangeText.Foreground = new SolidColorBrush(Color.FromRgb(22, 163, 74));
        ChangeText.Margin = new Thickness(8, 0, 0, 0);
        changePanel.Children.Add(ChangeText);
        Grid.SetRow(changePanel, 1);
        amountArea.Children.Add(changePanel);

        var charge = CreateActionButton("▣  CHARGE", 44, new SolidColorBrush(Color.FromRgb(32, 166, 74)), Brushes.White);
        charge.Click += Complete_Click;
        charge.Margin = new Thickness(0, 10, 0, 0);
        Grid.SetRow(charge, 2);
        amountArea.Children.Add(charge);

        Grid.SetRow(amountArea, 3);
        paymentContent.Children.Add(amountArea);

        StatusText.FontSize = 10;
        StatusText.Margin = new Thickness(0, 8, 0, 0);
        Grid.SetRow(StatusText, 4);
        paymentContent.Children.Add(StatusText);

        paymentBorder.Child = paymentContent;
        paymentGrid.Children.Add(paymentBorder);

        var right = new Grid();
        right.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        right.RowDefinitions.Add(new RowDefinition { Height = new GridLength(48) });
        Grid.SetColumn(right, 1);
        paymentGrid.Children.Add(right);

        var keypadBorder = new Border
        {
            Background = new SolidColorBrush(Color.FromRgb(246, 248, 251)),
            BorderBrush = new SolidColorBrush(Color.FromRgb(220, 226, 234)),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(5),
            Padding = new Thickness(4)
        };
        Grid.SetRow(keypadBorder, 0);

        var keypad = new UniformGrid { Rows = 4, Columns = 3 };
        foreach (var key in new[] { "7", "8", "9", "4", "5", "6", "1", "2", "3", ".", "0" })
            keypad.Children.Add(CreateKeypadButton(key));
        var backspace = CreateKeypadButton("⌫", null);
        backspace.Click += KeypadBackspace_Click;
        keypad.Children.Add(backspace);
        keypadBorder.Child = keypad;
        right.Children.Add(keypadBorder);

        var holdRecall = new Grid { Margin = new Thickness(0, 7, 0, 0) };
        holdRecall.ColumnDefinitions.Add(new ColumnDefinition());
        holdRecall.ColumnDefinitions.Add(new ColumnDefinition());
        Grid.SetRow(holdRecall, 1);

        var hold = CreateActionButton("Hold", 42, Brushes.White, new SolidColorBrush(Color.FromRgb(215, 222, 232)));
        hold.Margin = new Thickness(0, 0, 4, 0);
        hold.Click += Hold_Click;
        Grid.SetColumn(hold, 0);
        holdRecall.Children.Add(hold);

        var recall = CreateActionButton("Recall", 42, Brushes.White, new SolidColorBrush(Color.FromRgb(215, 222, 232)));
        recall.Margin = new Thickness(4, 0, 0, 0);
        recall.Click += Recall_Click;
        Grid.SetColumn(recall, 1);
        holdRecall.Children.Add(recall);

        right.Children.Add(holdRecall);
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
        var visible = tax > 0m;
        if (TaxText is not null) TaxText.Visibility = visible ? Visibility.Visible : Visibility.Collapsed;

        foreach (var textBlock in FindVisualChildren<TextBlock>(this))
        {
            if (string.Equals(textBlock.Text?.Trim(), "Tax", StringComparison.OrdinalIgnoreCase))
                textBlock.Visibility = visible ? Visibility.Visible : Visibility.Collapsed;
        }
    }

    private Button CreateKeypadButton(string value)
    {
        var button = new Button
        {
            Content = value,
            Tag = value,
            Style = (Style)FindResource("TouchKeyStyle")
        };
        if (value is not null) button.Click += Keypad_Click;
        return button;
    }

    private Button CreateActionButton(string text, double height, Brush background, Brush foreground)
    {
        return new Button
        {
            Content = text,
            Height = height,
            Background = background,
            Foreground = foreground,
            BorderBrush = background == Brushes.White ? new SolidColorBrush(Color.FromRgb(215, 222, 232)) : background,
            Style = (Style)FindResource("BaseButton"),
            HorizontalContentAlignment = HorizontalAlignment.Center,
            VerticalContentAlignment = VerticalAlignment.Center
        };
    }

    private static void ConfigurePaymentButton(Button button, string content, Thickness margin)
    {
        button.Content = content;
        button.Width = double.NaN;
        button.Height = 42;
        button.Margin = margin;
        button.HorizontalContentAlignment = HorizontalAlignment.Center;
        button.VerticalContentAlignment = VerticalAlignment.Center;
    }

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
