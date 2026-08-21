using POS_WPF.Domain.Common;

namespace POS_WPF.Domain.Returns;

public enum ReturnStatus { Draft, Completed, Cancelled }
public enum ReturnReason { CustomerReturn, Damaged, WrongItem, Defective, Other }

public sealed class SalesReturn : Entity
{
    private readonly List<SalesReturnLine> _lines = [];
    private SalesReturn() { }
    public SalesReturn(Guid branchId, Guid userId, string number, Guid? originalSaleId = null)
    { BranchId = branchId; UserId = userId; Number = number.Trim(); OriginalSaleId = originalSaleId; }
    public Guid BranchId { get; private set; }
    public Guid UserId { get; private set; }
    public Guid? OriginalSaleId { get; private set; }
    public string Number { get; private set; } = string.Empty;
    public ReturnStatus Status { get; private set; } = ReturnStatus.Draft;
    public ReturnReason Reason { get; private set; } = ReturnReason.CustomerReturn;
    public IReadOnlyCollection<SalesReturnLine> Lines => _lines;
    public decimal Total => _lines.Sum(x => x.Quantity * x.UnitPrice);

    public void AddLine(Guid productId, Guid unitId, decimal quantity, decimal historicalConversionFactor, decimal unitPrice)
    {
        if (quantity <= 0 || historicalConversionFactor <= 0 || unitPrice < 0) throw new ArgumentOutOfRangeException(nameof(quantity));
        _lines.Add(new SalesReturnLine(productId, unitId, quantity, historicalConversionFactor, unitPrice));
    }

    public void Complete(ReturnReason reason)
    {
        if (_lines.Count == 0) throw new InvalidOperationException("A return must contain at least one line.");
        if (Status != ReturnStatus.Draft) throw new InvalidOperationException("Only draft returns can be completed.");
        Reason = reason; Status = ReturnStatus.Completed; UpdatedAtUtc = DateTime.UtcNow;
    }
}

public sealed class SalesReturnLine
{
    private SalesReturnLine() { }
    internal SalesReturnLine(Guid productId, Guid unitId, decimal quantity, decimal historicalConversionFactor, decimal unitPrice)
    { Id = Guid.NewGuid(); ProductId = productId; UnitId = unitId; Quantity = quantity; HistoricalConversionFactor = historicalConversionFactor; UnitPrice = unitPrice; }
    public Guid Id { get; private set; }
    public Guid ProductId { get; private set; }
    public Guid UnitId { get; private set; }
    public decimal Quantity { get; private set; }
    public decimal HistoricalConversionFactor { get; private set; }
    public decimal BaseQuantity => Quantity * HistoricalConversionFactor;
    public decimal UnitPrice { get; private set; }
}
