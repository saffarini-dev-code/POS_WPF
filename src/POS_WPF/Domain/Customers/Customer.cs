using POS_WPF.Domain.Common;

namespace POS_WPF.Domain.Customers;

public sealed class Customer : Entity
{
    private Customer() { }
    public Customer(string code, string name) { Code = code.Trim(); Name = name.Trim(); }
    public string Code { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public string? NameArabic { get; private set; }
    public string? Phone { get; private set; }
    public string? Email { get; private set; }
    public string? Address { get; private set; }
    public Guid? CustomerGroupId { get; private set; }
    public decimal CreditLimit { get; private set; }
    public decimal CurrentBalance { get; private set; }
    public int LoyaltyPoints { get; private set; }
    public bool IsActive { get; private set; } = true;
    public void SetCreditLimit(decimal value) { if (value < 0) throw new ArgumentOutOfRangeException(nameof(value)); CreditLimit = value; UpdatedAtUtc = DateTime.UtcNow; }
    public void ApplyBalance(decimal delta) { var next = CurrentBalance + delta; if (next > CreditLimit && delta > 0) throw new InvalidOperationException("Customer credit limit exceeded."); CurrentBalance = next; UpdatedAtUtc = DateTime.UtcNow; }
    public void AddLoyaltyPoints(int points) { if (points < 0) throw new ArgumentOutOfRangeException(nameof(points)); LoyaltyPoints += points; UpdatedAtUtc = DateTime.UtcNow; }
}

public sealed class Supplier : Entity
{
    private Supplier() { }
    public Supplier(string code, string name) { Code = code.Trim(); Name = name.Trim(); }
    public string Code { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public string? Phone { get; private set; }
    public string? Email { get; private set; }
    public string? Address { get; private set; }
    public decimal CurrentBalance { get; private set; }
    public bool IsActive { get; private set; } = true;
    public void ApplyBalance(decimal delta) { CurrentBalance += delta; UpdatedAtUtc = DateTime.UtcNow; }
}
