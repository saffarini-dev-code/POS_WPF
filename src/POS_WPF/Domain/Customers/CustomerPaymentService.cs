using Microsoft.EntityFrameworkCore;
using POS_WPF.Data;

namespace POS_WPF.Domain.Customers;

public sealed class CustomerPaymentService(IDbContextFactory<AppDbContext> dbFactory)
{
    public async Task PostPaymentAsync(Guid customerId, decimal amount, Guid userId, string reference, CancellationToken cancellationToken = default)
    {
        if (amount <= 0) throw new ArgumentOutOfRangeException(nameof(amount));
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken); await using var tx = await db.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            var customer = await db.Customers.SingleAsync(x => x.Id == customerId && x.IsActive, cancellationToken);
            customer.ApplyBalance(-amount);
            db.AccountTransactions.Add(new AccountTransaction(AccountPartyType.Customer, customerId, AccountTransactionType.Payment, 0m, amount, reference));
            await db.SaveChangesAsync(cancellationToken); await tx.CommitAsync(cancellationToken);
        }
        catch { await tx.RollbackAsync(cancellationToken); throw; }
    }
}
