using Microsoft.EntityFrameworkCore;
using POS_WPF.Data;
using POS_WPF.Domain.Audit;
using POS_WPF.Domain.Inventory;
using POS_WPF.Domain.Sync;

namespace POS_WPF.Domain.Purchasing;

public sealed record PurchaseLineRequest(Guid ProductId, Guid UnitId, decimal Quantity, decimal UnitCost, decimal Discount, decimal Tax);
public sealed record PurchasePostingRequest(Guid BranchId, Guid WarehouseId, Guid SupplierId, Guid UserId, string Number, IReadOnlyList<PurchaseLineRequest> Lines);

public sealed class PurchasePostingService(IDbContextFactory<AppDbContext> dbFactory)
{
    public async Task<Guid> ReceiveAsync(PurchasePostingRequest request, CancellationToken cancellationToken = default)
    {
        if (request.Lines.Count == 0) throw new InvalidOperationException("The purchase must contain at least one line.");
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            var purchase = new Purchase(request.BranchId, request.WarehouseId, request.SupplierId, request.Number);
            foreach (var line in request.Lines)
            {
                var product = await db.Products.Include(x => x.Units).SingleAsync(x => x.Id == line.ProductId && x.IsActive, cancellationToken);
                var unit = product.Units.Single(x => x.Id == line.UnitId && x.IsActive && x.CanPurchase);
                purchase.AddLine(product.Id, unit.Id, line.Quantity, unit.ConversionFactorToBase, line.UnitCost, line.Discount, line.Tax);
                db.InventoryTransactions.Add(new InventoryTransaction
                {
                    ProductId = product.Id, UnitId = unit.Id, TransactionQuantity = line.Quantity,
                    ConversionFactor = unit.ConversionFactorToBase, BaseQuantity = line.Quantity * unit.ConversionFactorToBase,
                    TransactionType = InventoryTransactionType.Purchase, Reference = request.Number,
                    WarehouseId = request.WarehouseId, BranchId = request.BranchId, UserId = request.UserId
                });
            }
            purchase.MarkReceived();
            db.Purchases.Add(purchase);
            var supplier = await db.Suppliers.SingleAsync(x => x.Id == request.SupplierId, cancellationToken);
            supplier.ApplyBalance(purchase.Total);
            db.AuditEntries.Add(new AuditEntry(request.UserId, "Purchase.Received", nameof(Purchase), purchase.Id, null, $"Total={purchase.Total}", null, null, request.Number));
            db.SyncQueueEntries.Add(new SyncQueueEntry(nameof(Purchase), purchase.Id, "Create", request.Number));
            await db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return purchase.Id;
        }
        catch { await transaction.RollbackAsync(cancellationToken); throw; }
    }
}
