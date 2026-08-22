using POS_WPF.Domain.Common;

namespace POS_WPF.Domain.Stores;

public sealed class Branch : Entity
{
    private Branch() { }
    public Branch(string code, string name)
    {
        if (string.IsNullOrWhiteSpace(code)) throw new ArgumentException("Branch code is required.", nameof(code));
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Branch name is required.", nameof(name));
        Code = code.Trim(); Name = name.Trim();
    }
    public string Code { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public bool IsActive { get; private set; } = true;
}

public sealed class Warehouse : Entity
{
    private Warehouse() { }
    public Warehouse(Guid branchId, string code, string name)
    {
        BranchId = branchId; Code = code.Trim(); Name = name.Trim();
    }
    public Guid BranchId { get; private set; }
    public string Code { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public bool IsActive { get; private set; } = true;
}

public sealed class Terminal : Entity
{
    private Terminal() { }
    public Terminal(Guid branchId, string code, string name)
    {
        BranchId = branchId; Code = code.Trim(); Name = name.Trim();
    }
    public Guid BranchId { get; private set; }
    public string Code { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public bool IsActive { get; private set; } = true;
}

public sealed class CashRegister : Entity
{
    private CashRegister() { }
    public CashRegister(Guid branchId, string code, string name)
    {
        BranchId = branchId; Code = code.Trim(); Name = name.Trim();
    }
    public Guid BranchId { get; private set; }
    public string Code { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public bool IsOpen { get; private set; }
    public DateTime? OpenedAtUtc { get; private set; }
    public decimal OpeningBalance { get; private set; }

    public void Open(decimal openingBalance)
    {
        if (IsOpen) throw new InvalidOperationException("Cash register is already open.");
        if (openingBalance < 0) throw new ArgumentOutOfRangeException(nameof(openingBalance));
        OpeningBalance = openingBalance; IsOpen = true; OpenedAtUtc = DateTime.UtcNow;
    }

    public void Close()
    {
        if (!IsOpen) throw new InvalidOperationException("Cash register is already closed.");
        IsOpen = false;
        UpdatedAtUtc = DateTime.UtcNow;
    }
}
