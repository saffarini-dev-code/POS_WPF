using Microsoft.EntityFrameworkCore;
using POS_WPF.Data;

namespace POS_WPF.Domain.Customers;

public sealed class SupplierPaymentService(IDbContextFactory<AppDbContext> dbFactory)
{
    public async Task PostPaymentAsync(Guid supplierId, decimal amount, Guid userId, string reference, CancellationToken cancellationToken = default)
    {
        if (amount <= 0) throw new ArgumentOutOfRangeException(nameof(amount));
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken); await using var tx = await db.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            var supplier = await db.Suppliers.SingleAsync(x => x.Id == supplierId && x.IsActive, cancellationToken);
            supplier.ApplyBalance(-amount);
            db.AccountTransactions.Add(new AccountTransaction(AccountPartyType.Supplier, supplierId, AccountTransactionType.Payment, 0m, amount, reference));
            await db.SaveChangesAsync(cancellationToken); await tx.CommitAsync(cancellationToken);
        }
        catch { await tx.RollbackAsync(cancellationToken); throw; }
    }
}
