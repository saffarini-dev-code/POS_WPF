using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;

namespace POS_WPF;

/// <summary>
/// Final responsive layout pass for the cashier screen.
/// Keeps the cart/scan area compact and gives the touch payment keypad a dedicated,
/// predictable area so it never overlaps the payment controls.
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

        // Root: header / body / footer.
        if (root.RowDefinitions.Count < 3 || root.Children.Count == 0)
            return;

        if (root.Children.OfType<Grid>().FirstOrDefault(x => Grid.GetRow(x) == 1) is not Grid body)
            return;

        // Body: product area / cashier area.
        var cashierBorder = body.Children
            .OfType<Border>()
            .FirstOrDefault(x => Grid.GetColumn(x) == 1);

        if (cashierBorder?.Child is not Grid cashierGrid || cashierGrid.RowDefinitions.Count < 4)
            return;

        // Compact the order/scan area. The previous proportional layout made this
        // region consume too much vertical space and left the payment controls cramped.
        cashierGrid.RowDefinitions[0].Height = new GridLength(58);
        cashierGrid.RowDefinitions[1].Height = new GridLength(270);
        cashierGrid.RowDefinitions[1].MinHeight = 220;
        cashierGrid.RowDefinitions[2].Height = new GridLength(115);
        cashierGrid.RowDefinitions[3].Height = new GridLength(1, GridUnitType.Star);
        cashierGrid.RowDefinitions[3].MinHeight = 320;

        if (cashierGrid.Children
                .OfType<Grid>()
                .FirstOrDefault(x => Grid.GetRow(x) == 3) is not Grid paymentGrid)
            return;

        // Keep the keypad compact and proportional to the payment controls.
        // The previous 205px column with 52px buttons was visually oversized.
        if (paymentGrid.ColumnDefinitions.Count >= 2)
        {
            paymentGrid.ColumnDefinitions[0].Width = new GridLength(1, GridUnitType.Star);
            paymentGrid.ColumnDefinitions[1].Width = new GridLength(175);
        }

        paymentGrid.Margin = new Thickness(8, 6, 8, 6);

        var keypad = paymentGrid.Children
            .OfType<Grid>()
            .FirstOrDefault(x => Grid.GetColumn(x) == 1);

        if (keypad != null)
        {
            keypad.Margin = new Thickness(0);
            if (keypad.RowDefinitions.Count >= 2)
                keypad.RowDefinitions[1].Height = new GridLength(36);

            var keypadBorder = keypad.Children
                .OfType<Border>()
                .FirstOrDefault(x => Grid.GetRow(x) == 0);

            if (keypadBorder?.Child is UniformGrid uniformGrid)
            {
                keypadBorder.Padding = new Thickness(3);
                uniformGrid.Margin = new Thickness(0);

                foreach (var button in uniformGrid.Children.OfType<Button>())
                {
                    button.Height = 42;
                    button.MinHeight = 42;
                    button.Margin = new Thickness(2);
                    button.FontSize = 16;
                    button.Padding = new Thickness(1);
                }
            }
        }

        // Payment controls get consistent touch-friendly sizing without consuming
        // the space needed by the keypad.
        foreach (var button in paymentGrid.Children
                     .OfType<StackPanel>()
                     .SelectMany(x => x.Children.OfType<Button>()))
        {
            button.MinHeight = Math.Max(button.MinHeight, 34);
        }

        // The charge action should remain a clear, full-width touch target.
        var chargeButton = FindDescendant<Button>(paymentGrid, "ChargeButton");
        if (chargeButton != null)
        {
            chargeButton.Height = 38;
            chargeButton.MinHeight = 38;
        }
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
