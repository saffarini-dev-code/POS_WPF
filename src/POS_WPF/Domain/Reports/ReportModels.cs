namespace POS_WPF.Domain.Reports;

public sealed record ReportDateRange
{
    public ReportDateRange(DateTime fromUtc, DateTime toUtc)
    {
        if (toUtc < fromUtc) throw new ArgumentException("Report end must be after report start.");
        FromUtc = fromUtc;
        ToUtc = toUtc;
    }
    public DateTime FromUtc { get; }
    public DateTime ToUtc { get; }
}

public sealed record SalesReportRow(DateTime Date, string DocumentNumber, string Product, string Unit, decimal Quantity, decimal NetSales, decimal Tax, decimal Total);
public sealed record InventoryReportRow(Guid ProductId, string Product, string BaseUnit, decimal BaseQuantity, decimal AverageCost, decimal StockValue);
public sealed record CashReportRow(DateTime Date, string Type, string Reference, decimal Amount);
