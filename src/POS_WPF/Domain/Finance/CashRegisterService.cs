using Microsoft.EntityFrameworkCore;
using POS_WPF.Data;

namespace POS_WPF.Domain.Finance;

public sealed class CashRegisterService(IDbContextFactory<AppDbContext> dbFactory)
{
    public async Task<RegisterReconciliation> GetReconciliationAsync(Guid registerId, CancellationToken cancellationToken = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        var register = await db.CashRegisters.AsNoTracking().SingleAsync(x => x.Id == registerId, cancellationToken);
        var movements = await db.CashMovements.AsNoTracking().Where(x => x.RegisterId == registerId).ToListAsync(cancellationToken);
        return new RegisterReconciliation(
            register.OpeningBalance,
            movements.Where(x => x.Type == CashMovementType.Sale).Sum(x => x.Amount),
            movements.Where(x => x.Type == CashMovementType.Refund).Sum(x => x.Amount),
            movements.Where(x => x.Type == CashMovementType.Deposit).Sum(x => x.Amount),
            movements.Where(x => x.Type == CashMovementType.Withdrawal).Sum(x => x.Amount),
            movements.Where(x => x.Type == CashMovementType.Expense).Sum(x => x.Amount),
            0m);
    }

    public async Task<decimal> GetExpectedCashAsync(Guid registerId, CancellationToken cancellationToken = default)
        => (await GetReconciliationAsync(registerId, cancellationToken)).CalculateExpected();
}
