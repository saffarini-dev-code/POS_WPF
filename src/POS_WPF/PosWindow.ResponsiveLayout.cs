using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Threading;

namespace POS_WPF;

/// <summary>
/// Applies the cashier layout after WPF has completed the initial measure/arrange pass.
/// The registration is performed by a static field initializer so this partial class
/// does not declare a second static PosWindow constructor.
/// </summary>
public partial class PosWindow
{
    private static readonly bool _responsiveLayoutRegistered = RegisterResponsiveLayoutHandler();

    private static bool RegisterResponsiveLayoutHandler()
    {
        EventManager.RegisterClassHandler(
            typeof(PosWindow),
            FrameworkElement.LoadedEvent,
            new RoutedEventHandler(OnPosWindowLoadedForResponsiveLayout));
        return true;
    }

    private static void OnPosWindowLoadedForResponsiveLayout(object sender, RoutedEventArgs e)
    {
        if (sender is not PosWindow window)
            return;

        // Never mutate the layout tree while Loaded is being raised. Let WPF finish
        // the current Loaded/measure/arrange cycle first.
        window.Dispatcher.BeginInvoke(
            DispatcherPriority.ContextIdle,
            new Action(window.ApplyResponsiveLayoutSafely));
    }

    private void ApplyResponsiveLayoutSafely()
    {
        try
        {
            if (!IsLoaded || Content is not Grid root)
                return;

            if (root.RowDefinitions.Count < 3)
                return;

            var body = root.Children
                .OfType<Grid>()
                .FirstOrDefault(x => Grid.GetRow(x) == 1);

            if (body is null)
                return;

            var cashierBorder = body.Children
                .OfType<Border>()
                .FirstOrDefault(x => Grid.GetColumn(x) == 1);

            if (cashierBorder?.Child is not Grid cashierGrid || cashierGrid.RowDefinitions.Count < 4)
                return;

            // Keep the scan/cart region flexible and the payment area compact.
            cashierGrid.RowDefinitions[0].Height = new GridLength(58);
            cashierGrid.RowDefinitions[1].Height = new GridLength(1, GridUnitType.Star);
            cashierGrid.RowDefinitions[1].MinHeight = 0;
            cashierGrid.RowDefinitions[2].Height = new GridLength(115);
            cashierGrid.RowDefinitions[3].Height = new GridLength(270);
            cashierGrid.RowDefinitions[3].MinHeight = 270;
            cashierGrid.RowDefinitions[3].MaxHeight = 270;

            var paymentGrid = cashierGrid.Children
                .OfType<Grid>()
                .FirstOrDefault(x => Grid.GetRow(x) == 3);

            if (paymentGrid is null)
                return;

            if (paymentGrid.ColumnDefinitions.Count >= 2)
            {
                paymentGrid.ColumnDefinitions[0].Width = new GridLength(1, GridUnitType.Star);
                paymentGrid.ColumnDefinitions[1].Width = new GridLength(160);
            }

            paymentGrid.Margin = new Thickness(8, 6, 8, 6);

            // Payment order: Cash, Card, Mobile.
            var cardButton = FindDescendant<Button>(paymentGrid, "CardButton");
            var cashButton = FindDescendant<Button>(paymentGrid, "CashButton");
            var mobileButton = FindDescendant<Button>(paymentGrid, "MobileButton");
            if (cashButton != null && cardButton != null && mobileButton != null)
            {
                Grid.SetColumn(cashButton, 0);
                Grid.SetColumn(cardButton, 1);
                Grid.SetColumn(mobileButton, 2);
            }

            var keypad = paymentGrid.Children
                .OfType<Grid>()
                .FirstOrDefault(x => Grid.GetColumn(x) == 1);

            if (keypad != null)
            {
                keypad.Width = 160;
                keypad.Height = 250;
                keypad.MinHeight = 0;
                keypad.MaxHeight = 250;
                keypad.VerticalAlignment = VerticalAlignment.Top;
                keypad.HorizontalAlignment = HorizontalAlignment.Stretch;
                keypad.Margin = new Thickness(0);

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

            // Cash remains the default after every Loaded handler has completed.
            SelectPaymentMethod("Cash");
        }
        catch (Exception ex)
        {
            // Layout polish must never be allowed to terminate POS startup.
            try
            {
                Status("Ready", true);
            }
            catch
            {
                // Ignore secondary UI errors while recovering from layout problems.
            }

            System.Diagnostics.Debug.WriteLine(
                $"[ResponsiveLayout] {ex.GetType().Name}: {ex.Message}");
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
