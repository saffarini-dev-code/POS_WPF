using System.Globalization;
using System.Windows;

namespace POS_WPF;

public partial class PaymentConfirmationDialog : Window
{
    public PaymentConfirmationDialog(decimal amountDue, decimal cashReceived, string paymentMethod, Window owner)
    {
        InitializeComponent();
        Owner = owner;
        TotalText.Text = amountDue.ToString("C2", CultureInfo.CurrentCulture);
        AmountDueText.Text = amountDue.ToString("C2", CultureInfo.CurrentCulture);
        CashReceivedText.Text = cashReceived.ToString("C2", CultureInfo.CurrentCulture);
        ChangeText.Text = Math.Max(0m, cashReceived - amountDue).ToString("C2", CultureInfo.CurrentCulture);
        PaymentMethodText.Text = paymentMethod.ToUpperInvariant();
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private void CompleteSale_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = true;
        Close();
    }
}
