using Microsoft.EntityFrameworkCore;
using POS_WPF.Data;
using POS_WPF.Domain.Audit;
using POS_WPF.Domain.Inventory;
using POS_WPF.Domain.Sync;

namespace POS_WPF.Domain.Returns;

public sealed record SalesReturnPostingLine(Guid ProductId, Guid UnitId, decimal Quantity, decimal HistoricalConversionFactor, decimal UnitPrice);
public sealed record SalesReturnPostingRequest(Guid BranchId, Guid WarehouseId, Guid UserId, Guid TerminalId, string Number, Guid? OriginalSaleId, ReturnReason Reason, IReadOnlyList<SalesReturnPostingLine> Lines);

public sealed class SalesReturnPostingService(IDbContextFactory<AppDbContext> dbFactory)
{
    public async Task<Guid> PostAsync(SalesReturnPostingRequest request, CancellationToken cancellationToken = default)
    {
        if (request.Lines.Count == 0) throw new InvalidOperationException("The return must contain at least one line.");
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            var result = new SalesReturn(request.BranchId, request.UserId, request.Number, request.OriginalSaleId);
            foreach (var line in request.Lines)
            {
                if (line.Quantity <= 0 || line.HistoricalConversionFactor <= 0) throw new ArgumentOutOfRangeException(nameof(line));
                var product = await db.Products.SingleAsync(x => x.Id == line.ProductId && x.IsActive, cancellationToken);
                var unit = await db.ProductUnits.SingleAsync(x => x.Id == line.UnitId && x.ProductId == product.Id && x.IsActive, cancellationToken);
                result.AddLine(product.Id, unit.Id, line.Quantity, line.HistoricalConversionFactor, line.UnitPrice);
                db.InventoryTransactions.Add(new InventoryTransaction
                {
                    ProductId = product.Id, UnitId = unit.Id, TransactionQuantity = line.Quantity,
                    ConversionFactor = line.HistoricalConversionFactor, BaseQuantity = line.Quantity * line.HistoricalConversionFactor,
                    TransactionType = InventoryTransactionType.SalesReturn, Reference = request.Number,
                    WarehouseId = request.WarehouseId, BranchId = request.BranchId, UserId = request.UserId
                });
            }
            result.Complete(request.Reason);
            db.SalesReturns.Add(result);
            db.AuditEntries.Add(new AuditEntry(request.UserId, "Sale.Returned", nameof(SalesReturn), result.Id, null, $"Total={result.Total}", request.Reason.ToString(), request.TerminalId, request.Number));
            db.SyncQueueEntries.Add(new SyncQueueEntry(nameof(SalesReturn), result.Id, "Create", request.Number));
            await db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return result.Id;
        }
        catch { await transaction.RollbackAsync(cancellationToken); throw; }
    }
}
