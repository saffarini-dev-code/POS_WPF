using POS_WPF.Domain.Common;

namespace POS_WPF.Domain.Customers;

public enum AccountPartyType { Customer, Supplier }
public enum AccountTransactionType { Invoice, Payment, Return, OpeningBalance, Adjustment }

public sealed class AccountTransaction : Entity
{
    private AccountTransaction() { }
    public AccountTransaction(AccountPartyType partyType, Guid partyId, AccountTransactionType type, decimal debit, decimal credit, string reference, string? notes = null)
    {
        if (debit < 0 || credit < 0 || debit > 0 && credit > 0) throw new ArgumentOutOfRangeException(nameof(debit));
        PartyType = partyType; PartyId = partyId; Type = type; Debit = debit; Credit = credit; Reference = reference.Trim(); Notes = notes;
    }
    public AccountPartyType PartyType { get; private set; }
    public Guid PartyId { get; private set; }
    public AccountTransactionType Type { get; private set; }
    public decimal Debit { get; private set; }
    public decimal Credit { get; private set; }
    public string Reference { get; private set; } = string.Empty;
    public string? Notes { get; private set; }
    public decimal Net => Debit - Credit;
}
