namespace POS_WPF.Domain.Reports;

public sealed record ReportDateRange(DateTime FromUtc, DateTime ToUtc)
{
    public ReportDateRange(DateTime fromUtc, DateTime toUtc) : this()
    {
        if (toUtc < fromUtc) throw new ArgumentException("Report end must be after report start.");
    }
}

public sealed record SalesReportRow(DateTime Date, string DocumentNumber, string Product, string Unit, decimal Quantity, decimal NetSales, decimal Tax, decimal Total);
public sealed record InventoryReportRow(Guid ProductId, string Product, string BaseUnit, decimal BaseQuantity, decimal AverageCost, decimal StockValue);
public sealed record CashReportRow(DateTime Date, string Type, string Reference, decimal Amount);
