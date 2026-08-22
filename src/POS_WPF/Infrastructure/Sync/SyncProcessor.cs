using Microsoft.EntityFrameworkCore;
using POS_WPF.Data;
using POS_WPF.Domain.Sync;

namespace POS_WPF.Infrastructure.Sync;

public sealed class SyncProcessor(IDbContextFactory<AppDbContext> dbFactory, ISyncTransport transport)
{
    public async Task<int> ProcessAsync(int batchSize = 50, CancellationToken cancellationToken = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        var entries = await db.SyncQueueEntries.Where(x => x.Status == SyncStatus.Pending || x.Status == SyncStatus.Failed).OrderBy(x => x.CreatedAtUtc).Take(batchSize).ToListAsync(cancellationToken);
        if (entries.Count == 0) return 0;
        foreach (var entry in entries) entry.MarkProcessing();
        await db.SaveChangesAsync(cancellationToken);
        var batch = entries.Select(x => new SyncBatchItem(x.Id, x.EntityType, x.EntityId, x.Operation, x.Payload, x.CreatedAtUtc)).ToList();
        try
        {
            var conflicts = await transport.PushAsync(batch, cancellationToken);
            var conflictIds = conflicts.Select(x => x.QueueEntryId).ToHashSet();
            foreach (var entry in entries)
            {
                if (conflictIds.Contains(entry.Id)) entry.MarkFailed("Synchronization conflict requires manual resolution.");
                else entry.MarkSucceeded();
            }
        }
        catch (Exception ex)
        {
            foreach (var entry in entries) entry.MarkFailed(ex.Message);
        }
        await db.SaveChangesAsync(cancellationToken);
        return entries.Count;
    }
}
