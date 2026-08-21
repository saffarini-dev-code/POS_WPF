namespace POS_WPF.Domain.Inventory;

public enum InventoryTransactionType
{
    OpeningStock,
    Purchase,
    Sale,
    SalesReturn,
    PurchaseReturn,
    Adjustment,
    TransferIn,
    TransferOut,
    Damaged,
    Expired,
    Reservation,
    ReleaseReservation
}

public sealed class InventoryTransaction
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ProductId { get; set; }
    public Guid UnitId { get; set; }
    public decimal TransactionQuantity { get; set; }
    public decimal ConversionFactor { get; set; }
    public decimal BaseQuantity { get; set; }
    public InventoryTransactionType TransactionType { get; set; }
    public string? Reference { get; set; }
    public Guid? WarehouseId { get; set; }
    public Guid? BranchId { get; set; }
    public Guid? UserId { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
