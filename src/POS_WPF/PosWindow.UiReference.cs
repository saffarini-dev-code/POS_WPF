using System.Windows;
using System.Windows.Input;

namespace POS_WPF;

public partial class PosWindow
{
    private void InvoiceDiscountBox_GotKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
    {
        KeypadTarget_GotKeyboardFocus(sender, e);
        if (InvoiceDiscountBox.Text == "0.00")
        {
            InvoiceDiscountBox.Clear();
        }
    }

    private void InvoiceDiscountBox_LostKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(InvoiceDiscountBox.Text))
        {
            InvoiceDiscountBox.Text = "0.00";
        }
    }
}
