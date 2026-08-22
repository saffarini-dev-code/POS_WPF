using System.Globalization;
using System.Windows;

namespace POS_WPF;

public partial class PaymentConfirmationDialog : Window
{
    private readonly Func<Task<bool>> _completeSaleAsync;

    public PaymentConfirmationDialog(decimal amountDue, decimal cashReceived, string paymentMethod, Window owner, Func<Task<bool>> completeSaleAsync)
    {
        InitializeComponent();
        Owner = owner;
        _completeSaleAsync = completeSaleAsync;
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

    private async void CompleteSale_Click(object sender, RoutedEventArgs e)
    {
        CompleteButton.IsEnabled = false;
        CancelButton.IsEnabled = false;
        try
        {
            var completed = await _completeSaleAsync();
            if (completed)
            {
                DialogResult = true;
                Close();
                return;
            }
        }
        catch
        {
            // The POS sale pipeline reports the actual error through its toast/log.
        }
        finally
        {
            if (IsVisible)
            {
                CompleteButton.IsEnabled = true;
                CancelButton.IsEnabled = true;
            }
        }
    }
}
