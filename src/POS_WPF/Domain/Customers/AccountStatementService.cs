using Microsoft.EntityFrameworkCore;
using POS_WPF.Data;

namespace POS_WPF.Domain.Customers;

public sealed record AccountStatementRow(DateTime Date, AccountTransactionType Type, string Reference, decimal Debit, decimal Credit, decimal Balance, string? Notes);

public sealed class AccountStatementService(IDbContextFactory<AppDbContext> dbFactory)
{
    public async Task<IReadOnlyList<AccountStatementRow>> GetAsync(AccountPartyType partyType, Guid partyId, DateTime fromUtc, DateTime toUtc, CancellationToken cancellationToken = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        var rows = await db.AccountTransactions.AsNoTracking().Where(x => x.PartyType == partyType && x.PartyId == partyId && x.CreatedAtUtc >= fromUtc && x.CreatedAtUtc <= toUtc).OrderBy(x => x.CreatedAtUtc).Select(x => new { x.CreatedAtUtc, x.Type, x.Reference, x.Debit, x.Credit, x.Notes }).ToListAsync(cancellationToken);
        decimal balance = 0m;
        return rows.Select(x => { balance += x.Debit - x.Credit; return new AccountStatementRow(x.CreatedAtUtc, x.Type, x.Reference, x.Debit, x.Credit, balance, x.Notes); }).ToList();
    }
}
