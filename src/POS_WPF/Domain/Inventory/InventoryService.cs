using POS_WPF.Domain.Products;

namespace POS_WPF.Domain.Inventory;

public sealed class InventoryService(UnitConversionService conversionService)
{
    public InventoryTransaction CreateMovement(
        Product product,
        ProductUnit unit,
        decimal quantity,
        InventoryTransactionType transactionType,
        string? reference = null,
        Guid? warehouseId = null,
        Guid? branchId = null,
        Guid? userId = null)
    {
        if (unit.ProductId != product.Id)
            throw new InvalidOperationException("The selected unit does not belong to the product.");

        var baseQuantity = conversionService.ToBaseQuantity(quantity, unit);
        var signedBaseQuantity = transactionType is InventoryTransactionType.Sale
            or InventoryTransactionType.PurchaseReturn
            or InventoryTransactionType.TransferOut
            or InventoryTransactionType.Damaged
            or InventoryTransactionType.Expired
            ? -baseQuantity
            : baseQuantity;

        return new InventoryTransaction
        {
            ProductId = product.Id,
            UnitId = unit.Id,
            TransactionQuantity = quantity,
            ConversionFactor = unit.ConversionFactorToBase,
            BaseQuantity = signedBaseQuantity,
            TransactionType = transactionType,
            Reference = reference,
            WarehouseId = warehouseId,
            BranchId = branchId,
            UserId = userId
        };
    }
}
