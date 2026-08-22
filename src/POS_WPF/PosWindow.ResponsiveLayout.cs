using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;

namespace POS_WPF;

/// <summary>
/// Responsive layout pass for the cashier screen.
/// Keeps the order area compact and gives the payment controls and keypad
/// a balanced, predictable touch layout.
/// </summary>
public partial class PosWindow
{
    static PosWindow()
    {
        EventManager.RegisterClassHandler(
            typeof(PosWindow),
            FrameworkElement.LoadedEvent,
            new RoutedEventHandler(OnPosWindowLoadedForResponsiveLayout));
    }

    private static void OnPosWindowLoadedForResponsiveLayout(object sender, RoutedEventArgs e)
    {
        if (sender is not PosWindow window || window.Content is not Grid root)
            return;

        if (root.RowDefinitions.Count < 3 || root.Children.Count == 0)
            return;

        if (root.Children.OfType<Grid>().FirstOrDefault(x => Grid.GetRow(x) == 1) is not Grid body)
            return;

        var cashierBorder = body.Children
            .OfType<Border>()
            .FirstOrDefault(x => Grid.GetColumn(x) == 1);

        if (cashierBorder?.Child is not Grid cashierGrid || cashierGrid.RowDefinitions.Count < 4)
            return;

        // Keep the order/scan region compact. Do not let it consume the payment area.
        cashierGrid.RowDefinitions[0].Height = new GridLength(58);
        cashierGrid.RowDefinitions[1].Height = new GridLength(270);
        cashierGrid.RowDefinitions[1].MinHeight = 220;
        cashierGrid.RowDefinitions[2].Height = new GridLength(115);
        cashierGrid.RowDefinitions[3].Height = new GridLength(1, GridUnitType.Star);
        cashierGrid.RowDefinitions[3].MinHeight = 270;

        if (cashierGrid.Children
                .OfType<Grid>()
                .FirstOrDefault(x => Grid.GetRow(x) == 3) is not Grid paymentGrid)
            return;

        // Balanced payment split: keypad is intentionally compact, not full-height.
        if (paymentGrid.ColumnDefinitions.Count >= 2)
        {
            paymentGrid.ColumnDefinitions[0].Width = new GridLength(1, GridUnitType.Star);
            paymentGrid.ColumnDefinitions[1].Width = new GridLength(170);
        }

        paymentGrid.Margin = new Thickness(8, 6, 8, 6);

        var keypad = paymentGrid.Children
            .OfType<Grid>()
            .FirstOrDefault(x => Grid.GetColumn(x) == 1);

        if (keypad != null)
        {
            // Do NOT stretch the keypad vertically. The payment panel has more
            // height than the keypad needs, so the keypad stays compact at the top.
            keypad.Width = 170;
            keypad.Height = 250;
            keypad.MinHeight = 0;
            keypad.MaxHeight = 250;
            keypad.VerticalAlignment = VerticalAlignment.Top;
            keypad.HorizontalAlignment = HorizontalAlignment.Stretch;
            keypad.Margin = new Thickness(0);

            // Four compact number rows + Hold/Recall below.
            if (keypad.RowDefinitions.Count >= 2)
            {
                keypad.RowDefinitions[0].Height = new GridLength(210);
                keypad.RowDefinitions[1].Height = new GridLength(40);
            }

            var keypadBorder = keypad.Children
                .OfType<Border>()
                .FirstOrDefault(x => Grid.GetRow(x) == 0);

            if (keypadBorder?.Child is UniformGrid uniformGrid)
            {
                keypadBorder.Height = 210;
                keypadBorder.MinHeight = 0;
                keypadBorder.MaxHeight = 210;
                keypadBorder.Padding = new Thickness(3);
                uniformGrid.Height = 204;
                uniformGrid.MinHeight = 0;
                uniformGrid.MaxHeight = 204;
                uniformGrid.Margin = new Thickness(0);

                foreach (var button in uniformGrid.Children.OfType<Button>())
                {
                    button.Height = 42;
                    button.MinHeight = 42;
                    button.MaxHeight = 42;
                    button.Margin = new Thickness(2);
                    button.FontSize = 16;
                    button.Padding = new Thickness(1);
                    button.VerticalAlignment = VerticalAlignment.Center;
                }
            }

            foreach (var button in keypad.Children.OfType<Button>())
            {
                button.Height = 36;
                button.MinHeight = 36;
                button.MaxHeight = 36;
            }
        }

        foreach (var button in paymentGrid.Children
                     .OfType<StackPanel>()
                     .SelectMany(x => x.Children.OfType<Button>()))
        {
            button.MinHeight = Math.Max(button.MinHeight, 34);
        }

        var chargeButton = FindDescendant<Button>(paymentGrid, "ChargeButton");
        if (chargeButton != null)
        {
            chargeButton.Height = 38;
            chargeButton.MinHeight = 38;
        }

        // Cash is the first and default payment method. Schedule this after the
        // instance Loaded handler so the existing payment initialization cannot
        // switch it back to Card.
        window.Dispatcher.BeginInvoke(
            DispatcherPriority.Loaded,
            new Action(() => window.SelectPaymentMethod("Cash")));
    }

    private static T? FindDescendant<T>(DependencyObject root, string name) where T : FrameworkElement
    {
        if (root is T element && element.Name == name)
            return element;

        for (var i = 0; i < VisualTreeHelper.GetChildrenCount(root); i++)
        {
            var result = FindDescendant<T>(VisualTreeHelper.GetChild(root, i), name);
            if (result != null)
                return result;
        }

        return null;
    }
}
