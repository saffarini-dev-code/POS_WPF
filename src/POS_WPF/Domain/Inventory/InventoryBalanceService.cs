using Microsoft.EntityFrameworkCore;
using POS_WPF.Data;

namespace POS_WPF.Domain.Inventory;

public sealed record InventoryBalance(Guid ProductId, Guid? WarehouseId, decimal BaseQuantity);

public sealed class InventoryBalanceService(IDbContextFactory<AppDbContext> dbFactory)
{
    public async Task<decimal> GetBaseBalanceAsync(Guid productId, Guid? warehouseId = null, CancellationToken cancellationToken = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        var query = db.InventoryTransactions.AsNoTracking().Where(x => x.ProductId == productId);
        if (warehouseId.HasValue) query = query.Where(x => x.WarehouseId == warehouseId.Value);
        return await query.SumAsync(x => (decimal?)x.BaseQuantity, cancellationToken) ?? 0m;
    }

    public async Task<IReadOnlyList<InventoryBalance>> GetBalancesAsync(CancellationToken cancellationToken = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        return await db.InventoryTransactions.AsNoTracking()
            .GroupBy(x => new { x.ProductId, x.WarehouseId })
            .Select(g => new InventoryBalance(g.Key.ProductId, g.Key.WarehouseId, g.Sum(x => x.BaseQuantity)))
            .OrderBy(x => x.ProductId)
            .ToListAsync(cancellationToken);
    }
}
