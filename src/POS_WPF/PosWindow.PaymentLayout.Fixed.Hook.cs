using System.Windows;

namespace POS_WPF;

public partial class PosWindow
{
    private static readonly object FixedPaymentLayoutRegistration = RegisterFixedPaymentLayout();

    private static object RegisterFixedPaymentLayout()
    {
        EventManager.RegisterClassHandler(typeof(PosWindow), FrameworkElement.LoadedEvent, new RoutedEventHandler(OnFixedPaymentLayoutLoaded));
        return new object();
    }

    private static void OnFixedPaymentLayoutLoaded(object sender, RoutedEventArgs e)
    {
        if (sender is PosWindow window)
        {
            window.ApplyFixedPaymentLayout();
            window.HookFixedTaxVisibility();
        }
    }
}
