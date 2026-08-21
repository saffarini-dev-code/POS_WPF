using Microsoft.EntityFrameworkCore;
using POS_WPF.Data;
using POS_WPF.Domain.Audit;
using POS_WPF.Domain.Sync;
using POS_WPF.Infrastructure.Security;

namespace POS_WPF.Domain.Inventory;

public sealed record InventoryAdjustmentRequest(Guid BranchId, Guid WarehouseId, Guid UserId, Guid ProductId, Guid UnitId, decimal Quantity, bool Increase, string Reason, string Reference);
public sealed record InventoryTransferRequest(Guid BranchId, Guid SourceWarehouseId, Guid DestinationWarehouseId, Guid UserId, Guid ProductId, Guid UnitId, decimal Quantity, string Reference);

public sealed class InventoryOperationsService(IDbContextFactory<AppDbContext> dbFactory, InventoryService inventoryService, PermissionService permissions)
{
    public async Task<Guid> AdjustAsync(InventoryAdjustmentRequest request, CancellationToken cancellationToken = default)
    {
        await permissions.DemandAsync("Inventory.Adjust", cancellationToken);
        if (request.Quantity <= 0) throw new ArgumentOutOfRangeException(nameof(request.Quantity));
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken); var product = await db.Products.Include(x => x.Units).SingleAsync(x => x.Id == request.ProductId && x.IsActive, cancellationToken); var unit = product.Units.Single(x => x.Id == request.UnitId && x.IsActive);
        var movement = inventoryService.CreateMovement(product, unit, request.Quantity, request.Increase ? InventoryTransactionType.AdjustmentIn : InventoryTransactionType.AdjustmentOut, request.Reference, request.WarehouseId, request.BranchId, request.UserId); if (!request.Increase) movement.BaseQuantity = -Math.Abs(movement.BaseQuantity);
        db.InventoryTransactions.Add(movement); db.AuditEntries.Add(new AuditEntry(request.UserId, request.Increase ? "Inventory.AdjustmentIn" : "Inventory.AdjustmentOut", nameof(InventoryTransaction), movement.Id, null, $"Quantity={request.Quantity}", request.Reason, null, request.Reference)); db.SyncQueueEntries.Add(new SyncQueueEntry(nameof(InventoryTransaction), movement.Id, "Create", request.Reference)); await db.SaveChangesAsync(cancellationToken); return movement.Id;
    }
    public async Task TransferAsync(InventoryTransferRequest request, CancellationToken cancellationToken = default)
    {
        await permissions.DemandAsync("Inventory.Transfer", cancellationToken); if (request.Quantity <= 0 || request.SourceWarehouseId == request.DestinationWarehouseId) throw new ArgumentException("Invalid transfer request.");
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken); await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            var product = await db.Products.Include(x => x.Units).SingleAsync(x => x.Id == request.ProductId && x.IsActive, cancellationToken); var unit = product.Units.Single(x => x.Id == request.UnitId && x.IsActive); var available = await db.InventoryTransactions.Where(x => x.ProductId == request.ProductId && x.WarehouseId == request.SourceWarehouseId).SumAsync(x => (decimal?)x.BaseQuantity, cancellationToken) ?? 0m; var baseQuantity = request.Quantity * unit.ConversionFactorToBase;
            if (available < baseQuantity) throw new InvalidOperationException("Insufficient stock for transfer.");
            db.InventoryTransactions.Add(new InventoryTransaction { ProductId = product.Id, UnitId = unit.Id, TransactionQuantity = request.Quantity, ConversionFactor = unit.ConversionFactorToBase, BaseQuantity = -baseQuantity, TransactionType = InventoryTransactionType.TransferOut, Reference = request.Reference, WarehouseId = request.SourceWarehouseId, BranchId = request.BranchId, UserId = request.UserId }); db.InventoryTransactions.Add(new InventoryTransaction { ProductId = product.Id, UnitId = unit.Id, TransactionQuantity = request.Quantity, ConversionFactor = unit.ConversionFactorToBase, BaseQuantity = baseQuantity, TransactionType = InventoryTransactionType.TransferIn, Reference = request.Reference, WarehouseId = request.DestinationWarehouseId, BranchId = request.BranchId, UserId = request.UserId }); db.AuditEntries.Add(new AuditEntry(request.UserId, "Inventory.Transfer", nameof(InventoryTransaction), null, null, $"BaseQuantity={baseQuantity}", null, null, request.Reference)); await db.SaveChangesAsync(cancellationToken); await transaction.CommitAsync(cancellationToken);
        }
        catch { await transaction.RollbackAsync(cancellationToken); throw; }
    }
}
