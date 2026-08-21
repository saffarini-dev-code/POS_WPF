namespace POS_WPF.Domain.Inventory;

public sealed class OpeningStockService(InventoryOperationsService inventory)
{
    public Task<Guid> AddAsync(Guid branchId, Guid warehouseId, Guid userId, Guid productId, Guid unitId, decimal quantity, string reference, CancellationToken cancellationToken = default)
        => inventory.AdjustAsync(new InventoryAdjustmentRequest(branchId, warehouseId, userId, productId, unitId, quantity, true, "Opening Stock", reference), cancellationToken);
}
