using System.Globalization;
using System.IO;
using System.Text;
using System.Windows;
using Microsoft.Win32;
using POS_WPF.Domain.Reports;

namespace POS_WPF;

public partial class ReportsWindow : Window
{
    private readonly ReportQueryService _reports; private IReadOnlyList<SalesReportRow> _sales = []; private IReadOnlyList<InventoryReportRow> _inventory = [];
    public ReportsWindow(ReportQueryService reports) { InitializeComponent(); _reports = reports; FromDate.SelectedDate = DateTime.Today; ToDate.SelectedDate = DateTime.Today; }
    private async void Sales_Click(object sender, RoutedEventArgs e) { var from = (FromDate.SelectedDate ?? DateTime.Today).ToUniversalTime(); var to = (ToDate.SelectedDate ?? DateTime.Today).Date.AddDays(1).AddTicks(-1).ToUniversalTime(); _sales = await _reports.GetSalesAsync(new ReportDateRange(from, to)); SalesGrid.ItemsSource = _sales; }
    private async void Inventory_Click(object sender, RoutedEventArgs e) { _inventory = await _reports.GetInventoryAsync(); InventoryGrid.ItemsSource = _inventory; }
    private void Export_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new SaveFileDialog { Filter = "CSV files (*.csv)|*.csv", FileName = "POS-Report.csv" }; if (dialog.ShowDialog() != true) return;
        var lines = new List<string>();
        if (_sales.Count > 0) { lines.Add("Date,Document,Product,Unit,Quantity,NetSales,Tax,Total"); lines.AddRange(_sales.Select(x => string.Join(',', Csv(x.Date.ToString("O")), Csv(x.DocumentNumber), Csv(x.Product), Csv(x.Unit), x.Quantity.ToString(CultureInfo.InvariantCulture), x.NetSales.ToString(CultureInfo.InvariantCulture), x.Tax.ToString(CultureInfo.InvariantCulture), x.Total.ToString(CultureInfo.InvariantCulture)))); }
        else { lines.Add("Product,BaseUnit,BaseQuantity,AverageCost,StockValue"); lines.AddRange(_inventory.Select(x => string.Join(',', Csv(x.Product), Csv(x.BaseUnit), x.BaseQuantity.ToString(CultureInfo.InvariantCulture), x.AverageCost.ToString(CultureInfo.InvariantCulture), x.StockValue.ToString(CultureInfo.InvariantCulture)))); }
        File.WriteAllLines(dialog.FileName, lines, new UTF8Encoding(true));
    }
    private static string Csv(string value) => $"\"{value.Replace("\"", "\"\"") }\"";
}
