using POS_WPF.Domain.Common;

namespace POS_WPF.Domain.Purchasing;

public enum PurchaseStatus { Draft, Received, Cancelled, Returned }

public sealed class Purchase : Entity
{
    private readonly List<PurchaseLine> _lines = [];
    private Purchase() { }
    public Purchase(Guid branchId, Guid warehouseId, Guid supplierId, string number)
    { BranchId = branchId; WarehouseId = warehouseId; SupplierId = supplierId; Number = number.Trim(); }
    public Guid BranchId { get; private set; }
    public Guid WarehouseId { get; private set; }
    public Guid SupplierId { get; private set; }
    public string Number { get; private set; } = string.Empty;
    public PurchaseStatus Status { get; private set; } = PurchaseStatus.Draft;
    public decimal Subtotal { get; private set; }
    public decimal Discount { get; private set; }
    public decimal Tax { get; private set; }
    public decimal Total { get; private set; }
    public IReadOnlyCollection<PurchaseLine> Lines => _lines;
    public void AddLine(Guid productId, Guid unitId, decimal quantity, decimal conversionFactor, decimal unitCost, decimal discount, decimal tax)
    {
        if (quantity <= 0 || conversionFactor <= 0 || unitCost < 0 || discount < 0 || tax < 0) throw new ArgumentOutOfRangeException(nameof(quantity));
        _lines.Add(new PurchaseLine(productId, unitId, quantity, conversionFactor, unitCost, discount, tax));
        Subtotal = _lines.Sum(x => x.Quantity * x.UnitCost); Discount = _lines.Sum(x => x.Discount); Tax = _lines.Sum(x => x.Tax); Total = Math.Round(Subtotal - Discount + Tax, 2, MidpointRounding.AwayFromZero);
    }
    public void MarkReceived()
    {
        if (_lines.Count == 0) throw new InvalidOperationException("A purchase must contain at least one line.");
        if (Status != PurchaseStatus.Draft) throw new InvalidOperationException("Only draft purchases can be received.");
        Status = PurchaseStatus.Received; UpdatedAtUtc = DateTime.UtcNow;
    }
}

public sealed class PurchaseLine
{
    private PurchaseLine() { }
    internal PurchaseLine(Guid productId, Guid unitId, decimal quantity, decimal conversionFactor, decimal unitCost, decimal discount, decimal tax)
    { Id = Guid.NewGuid(); ProductId = productId; UnitId = unitId; Quantity = quantity; ConversionFactor = conversionFactor; UnitCost = unitCost; Discount = discount; Tax = tax; }
    public Guid Id { get; private set; }
    public Guid ProductId { get; private set; }
    public Guid UnitId { get; private set; }
    public decimal Quantity { get; private set; }
    public decimal ConversionFactor { get; private set; }
    public decimal BaseQuantity => Quantity * ConversionFactor;
    public decimal UnitCost { get; private set; }
    public decimal Discount { get; private set; }
    public decimal Tax { get; private set; }
}
