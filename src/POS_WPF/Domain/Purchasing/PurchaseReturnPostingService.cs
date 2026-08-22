using Microsoft.EntityFrameworkCore;
using POS_WPF.Data;
using POS_WPF.Domain.Audit;
using POS_WPF.Domain.Customers;
using POS_WPF.Domain.Inventory;
using POS_WPF.Domain.Sync;

namespace POS_WPF.Domain.Purchasing;

public sealed record PurchaseReturnLineRequest(Guid ProductId, Guid UnitId, decimal Quantity, decimal HistoricalConversionFactor, decimal UnitCost);
public sealed record PurchaseReturnRequest(Guid BranchId, Guid WarehouseId, Guid SupplierId, Guid UserId, string Number, IReadOnlyList<PurchaseReturnLineRequest> Lines);

public sealed class PurchaseReturnPostingService(IDbContextFactory<AppDbContext> dbFactory)
{
    public async Task<Guid> PostAsync(PurchaseReturnRequest request, CancellationToken cancellationToken = default)
    {
        if (request.Lines.Count == 0) throw new InvalidOperationException("The purchase return must contain at least one line.");
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        await using var tx = await db.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            decimal total = 0m;
            foreach (var line in request.Lines)
            {
                var product = await db.Products.SingleAsync(x => x.Id == line.ProductId && x.IsActive, cancellationToken);
                var unit = await db.ProductUnits.SingleAsync(x => x.Id == line.UnitId && x.ProductId == product.Id && x.IsActive, cancellationToken);
                var baseQty = line.Quantity * line.HistoricalConversionFactor;
                var available = await db.InventoryTransactions.Where(x => x.ProductId == product.Id && x.WarehouseId == request.WarehouseId).SumAsync(x => (decimal?)x.BaseQuantity, cancellationToken) ?? 0m;
                if (available < baseQty) throw new InvalidOperationException($"Insufficient stock for {product.Name}.");
                db.InventoryTransactions.Add(new InventoryTransaction { ProductId = product.Id, UnitId = unit.Id, TransactionQuantity = line.Quantity, ConversionFactor = line.HistoricalConversionFactor, BaseQuantity = -baseQty, TransactionType = InventoryTransactionType.PurchaseReturn, Reference = request.Number, WarehouseId = request.WarehouseId, BranchId = request.BranchId, UserId = request.UserId });
                total += line.Quantity * line.UnitCost;
            }
            var supplier = await db.Suppliers.SingleAsync(x => x.Id == request.SupplierId, cancellationToken);
            supplier.ApplyBalance(-total);
            db.AccountTransactions.Add(new AccountTransaction(AccountPartyType.Supplier, supplier.Id, AccountTransactionType.Return, 0m, total, request.Number));
            db.AuditEntries.Add(new AuditEntry(request.UserId, "Purchase.Returned", nameof(Purchase), null, null, $"Total={total}", null, null, request.Number));
            db.SyncQueueEntries.Add(new SyncQueueEntry(nameof(Purchase), Guid.NewGuid(), "Return", request.Number));
            await db.SaveChangesAsync(cancellationToken); await tx.CommitAsync(cancellationToken); return supplier.Id;
        }
        catch { await tx.RollbackAsync(cancellationToken); throw; }
    }
}
