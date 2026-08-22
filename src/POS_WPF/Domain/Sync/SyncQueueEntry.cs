using POS_WPF.Domain.Common;

namespace POS_WPF.Domain.Sync;

public enum SyncStatus { Pending, Processing, Succeeded, Failed }

public sealed class SyncQueueEntry : Entity
{
    private SyncQueueEntry() { }
    public SyncQueueEntry(string entityType, Guid entityId, string operation, string payload)
    {
        EntityType = entityType.Trim(); EntityId = entityId; Operation = operation.Trim(); Payload = payload;
    }
    public string EntityType { get; private set; } = string.Empty;
    public Guid EntityId { get; private set; }
    public string Operation { get; private set; } = string.Empty;
    public string Payload { get; private set; } = string.Empty;
    public SyncStatus Status { get; private set; } = SyncStatus.Pending;
    public int Attempts { get; private set; }
    public string? LastError { get; private set; }

    public void MarkProcessing() { Status = SyncStatus.Processing; Attempts++; }
    public void MarkSucceeded() { Status = SyncStatus.Succeeded; LastError = null; }
    public void MarkFailed(string error) { Status = SyncStatus.Failed; LastError = error; }
    public void Reset() { Status = SyncStatus.Pending; LastError = null; }
}
