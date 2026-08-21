using POS_WPF.Domain.Common;

namespace POS_WPF.Domain.Inventory;

public enum InventoryTransactionType
{
    OpeningStock,
    Purchase,
    PurchaseReturn,
    Sale,
    SalesReturn,
    Adjustment,
    TransferIn,
    TransferOut,
    Damaged,
    Expired,
    Reservation,
    ReservationRelease
}

public sealed class InventoryTransaction : Entity
{
    private InventoryTransaction()
    {
    }

    public InventoryTransaction(
        Guid productId,
        Guid unitId,
        decimal transactionQuantity,
        decimal conversionFactor,
        InventoryTransactionType transactionType,
        string referenceType,
        Guid referenceId,
        Guid warehouseId,
        Guid branchId,
        Guid userId)
    {
        if (productId == Guid.Empty) throw new ArgumentException("Product is required.", nameof(productId));
        if (unitId == Guid.Empty) throw new ArgumentException("Unit is required.", nameof(unitId));
        if (transactionQuantity == 0) throw new ArgumentException("Transaction quantity cannot be zero.", nameof(transactionQuantity));
        if (conversionFactor <= 0) throw new ArgumentOutOfRangeException(nameof(conversionFactor));

        ProductId = productId;
        UnitId = unitId;
        TransactionQuantity = transactionQuantity;
        ConversionFactor = conversionFactor;
        BaseQuantity = transactionQuantity * conversionFactor;
        TransactionType = transactionType;
        ReferenceType = referenceType;
        ReferenceId = referenceId;
        WarehouseId = warehouseId;
        BranchId = branchId;
        UserId = userId;
        OccurredAtUtc = DateTime.UtcNow;
    }

    public Guid ProductId { get; private set; }
    public Guid UnitId { get; private set; }
    public decimal TransactionQuantity { get; private set; }
    public decimal ConversionFactor { get; private set; }
    public decimal BaseQuantity { get; private set; }
    public InventoryTransactionType TransactionType { get; private set; }
    public string ReferenceType { get; private set; } = string.Empty;
    public Guid ReferenceId { get; private set; }
    public Guid WarehouseId { get; private set; }
    public Guid BranchId { get; private set; }
    public Guid UserId { get; private set; }
    public DateTime OccurredAtUtc { get; private set; }
    public string? Reason { get; private set; }

    public void SetReason(string? reason) => Reason = string.IsNullOrWhiteSpace(reason) ? null : reason.Trim();
}
