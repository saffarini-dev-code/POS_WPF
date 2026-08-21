using Microsoft.EntityFrameworkCore;
using POS_WPF.Data;

namespace POS_WPF.Domain.Reports;

public sealed class ReportQueryService(IDbContextFactory<AppDbContext> dbFactory)
{
    public async Task<IReadOnlyList<SalesReportRow>> GetSalesAsync(ReportDateRange range, Guid? branchId = null, CancellationToken cancellationToken = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        var query = db.Sales.AsNoTracking().Where(x => x.CreatedAtUtc >= range.FromUtc && x.CreatedAtUtc <= range.ToUtc && x.Status == Sales.SaleStatus.Completed);
        if (branchId.HasValue) query = query.Where(x => x.BranchId == branchId.Value);
        return await query.SelectMany(s => s.Lines.Select(l => new SalesReportRow(
            s.CreatedAtUtc, s.Number, l.Description, l.UnitId.ToString(), l.Quantity, l.Quantity * l.UnitPrice - l.Discount, l.Tax, l.Quantity * l.UnitPrice - l.Discount + l.Tax))).ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<InventoryReportRow>> GetInventoryAsync(CancellationToken cancellationToken = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        return await db.Products.AsNoTracking().Select(p => new InventoryReportRow(
            p.Id, p.Name, p.Units.Where(u => u.Id == p.BaseUnitId).Select(u => u.Abbreviation).FirstOrDefault() ?? "",
            db.InventoryTransactions.Where(t => t.ProductId == p.Id).Sum(t => (decimal?)t.BaseQuantity) ?? 0m,
            p.Units.Where(u => u.Id == p.BaseUnitId).Select(u => u.PurchasePrice).FirstOrDefault(),
            (db.InventoryTransactions.Where(t => t.ProductId == p.Id).Sum(t => (decimal?)t.BaseQuantity) ?? 0m) * p.Units.Where(u => u.Id == p.BaseUnitId).Select(u => u.PurchasePrice).FirstOrDefault())).ToListAsync(cancellationToken);
    }
}
