using POS_WPF.Domain.Common;

namespace POS_WPF.Domain.Sales;

public enum SaleStatus { Draft, Completed, Voided, Returned }

public sealed class Sale : Entity
{
    private readonly List<SaleLine> _lines = [];
    private readonly List<SalePayment> _payments = [];
    private Sale() { }
    public Sale(Guid branchId, Guid terminalId, Guid cashierId, string number)
    { BranchId = branchId; TerminalId = terminalId; CashierId = cashierId; Number = number.Trim(); }
    public Guid BranchId { get; private set; }
    public Guid TerminalId { get; private set; }
    public Guid CashierId { get; private set; }
    public Guid? CustomerId { get; private set; }
    public string Number { get; private set; } = string.Empty;
    public SaleStatus Status { get; private set; } = SaleStatus.Draft;
    public decimal Subtotal { get; private set; }
    public decimal Discount { get; private set; }
    public decimal Tax { get; private set; }
    public decimal Total { get; private set; }
    public IReadOnlyCollection<SaleLine> Lines => _lines;
    public IReadOnlyCollection<SalePayment> Payments => _payments;
    public void SetCustomer(Guid? customerId) => CustomerId = customerId;
    public void AddLine(Guid productId, Guid unitId, string description, decimal quantity, decimal conversionFactor, decimal unitPrice, decimal discount, decimal tax)
    {
        if (quantity <= 0 || conversionFactor <= 0 || unitPrice < 0 || discount < 0 || tax < 0) throw new ArgumentOutOfRangeException(nameof(quantity));
        _lines.Add(new SaleLine(productId, unitId, description, quantity, conversionFactor, unitPrice, discount, tax));
        Recalculate();
    }
    public void AddPayment(string method, decimal amount)
    {
        if (string.IsNullOrWhiteSpace(method) || amount <= 0) throw new ArgumentOutOfRangeException(nameof(amount));
        _payments.Add(new SalePayment(method, amount));
    }
    public void Complete()
    {
        if (_lines.Count == 0) throw new InvalidOperationException("A sale must contain at least one line.");
        if (Payments.Sum(x => x.Amount) < Total) throw new InvalidOperationException("Payment total is insufficient.");
        if (Status != SaleStatus.Draft) throw new InvalidOperationException("Only draft sales can be completed.");
        Status = SaleStatus.Completed; UpdatedAtUtc = DateTime.UtcNow;
    }
    private void Recalculate()
    {
        Subtotal = _lines.Sum(x => x.Quantity * x.UnitPrice);
        Discount = _lines.Sum(x => x.Discount);
        Tax = _lines.Sum(x => x.Tax);
        Total = Math.Round(Subtotal - Discount + Tax, 2, MidpointRounding.AwayFromZero);
    }
}

public sealed class SaleLine
{
    private SaleLine() { }
    internal SaleLine(Guid productId, Guid unitId, string description, decimal quantity, decimal conversionFactor, decimal unitPrice, decimal discount, decimal tax)
    { Id = Guid.NewGuid(); ProductId = productId; UnitId = unitId; Description = description.Trim(); Quantity = quantity; ConversionFactor = conversionFactor; UnitPrice = unitPrice; Discount = discount; Tax = tax; }
    public Guid Id { get; private set; }
    public Guid ProductId { get; private set; }
    public Guid UnitId { get; private set; }
    public string Description { get; private set; } = string.Empty;
    public decimal Quantity { get; private set; }
    public decimal ConversionFactor { get; private set; }
    public decimal BaseQuantity => Quantity * ConversionFactor;
    public decimal UnitPrice { get; private set; }
    public decimal Discount { get; private set; }
    public decimal Tax { get; private set; }
}

public sealed class SalePayment
{
    private SalePayment() { }
    internal SalePayment(string method, decimal amount) { Id = Guid.NewGuid(); Method = method.Trim(); Amount = amount; }
    public Guid Id { get; private set; }
    public string Method { get; private set; } = string.Empty;
    public decimal Amount { get; private set; }
}
