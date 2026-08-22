using POS_WPF.Domain.Sync;

namespace POS_WPF.Infrastructure.Sync;

public sealed record SyncBatchItem(Guid Id, string EntityType, Guid EntityId, string Operation, string Payload, DateTime CreatedAtUtc);
public sealed record SyncConflict(Guid QueueEntryId, string EntityType, Guid EntityId, string LocalPayload, string RemotePayload);
public enum SyncConflictResolution { PreferServer, PreferLocal, Manual }

public interface ISyncTransport
{
    Task<IReadOnlyList<SyncConflict>> PushAsync(IReadOnlyList<SyncBatchItem> batch, CancellationToken cancellationToken = default);
}

public interface ISyncConflictResolver
{
    SyncConflictResolution Resolve(SyncConflict conflict);
}

public sealed class DefaultSyncConflictResolver : ISyncConflictResolver
{
    public SyncConflictResolution Resolve(SyncConflict conflict) => SyncConflictResolution.Manual;
}
