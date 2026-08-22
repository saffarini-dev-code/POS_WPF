using System.Windows;
using System.Windows.Controls;

namespace POS_WPF;

public partial class PosWindow
{
    static PosWindow()
    {
        EventManager.RegisterClassHandler(typeof(PosWindow), FrameworkElement.LoadedEvent, new RoutedEventHandler(OnPosWindowLoaded));
    }

    private static void OnPosWindowLoaded(object sender, RoutedEventArgs e)
    {
        if (sender is not PosWindow window || window.Content is not Grid root || root.Children.Count < 2)
            return;

        // Keep the cashier layout proportional to the reference POS screen:
        // right order/payment panel = 30% of the content width (1.5x the old 20%).
        if (root.Children[1] is Grid content && content.ColumnDefinitions.Count >= 2)
        {
            content.ColumnDefinitions[0].Width = new GridLength(7, GridUnitType.Star);
            content.ColumnDefinitions[1].Width = new GridLength(3, GridUnitType.Star);

            if (content.Children.Count >= 2 && content.Children[1] is Border rightPanel && rightPanel.Child is Grid paymentGrid)
            {
                // Give the payment area enough vertical space for the complete touch keypad,
                // while reducing the empty "Scan or select items" area above it.
                if (paymentGrid.RowDefinitions.Count >= 4)
                {
                    paymentGrid.RowDefinitions[0].Height = new GridLength(58);
                    paymentGrid.RowDefinitions[1].Height = new GridLength(1, GridUnitType.Star);
                    paymentGrid.RowDefinitions[2].Height = new GridLength(138);
                    paymentGrid.RowDefinitions[3].Height = new GridLength(245);
                }
            }
        }
    }
}
