using POS_WPF.Domain.Common;

namespace POS_WPF.Domain.Finance;

public enum CashMovementType { Opening, Sale, Refund, Expense, Withdrawal, Deposit, ClosingAdjustment }

public sealed class CashMovement : Entity
{
    private CashMovement() { }
    public CashMovement(Guid registerId, CashMovementType type, decimal amount, string reference, Guid? userId = null)
    {
        if (amount < 0) throw new ArgumentOutOfRangeException(nameof(amount));
        RegisterId = registerId; Type = type; Amount = amount; Reference = reference.Trim(); UserId = userId;
    }
    public Guid RegisterId { get; private set; }
    public CashMovementType Type { get; private set; }
    public decimal Amount { get; private set; }
    public string Reference { get; private set; } = string.Empty;
    public Guid? UserId { get; private set; }
}

public sealed record RegisterReconciliation(decimal Opening, decimal Sales, decimal Refunds, decimal Deposits, decimal Withdrawals, decimal Expenses, decimal ExpectedCash)
{
    public decimal CalculateExpected() => Opening + Sales + Deposits - Refunds - Withdrawals - Expenses;
    public decimal CalculateVariance(decimal actualCash) => actualCash - CalculateExpected();
}
