using POS_WPF.Domain.Common;

namespace POS_WPF.Domain.Audit;

public sealed class AuditEntry : Entity
{
    private AuditEntry() { }

    public AuditEntry(Guid? userId, string action, string entityType, Guid? entityId, string? details)
    {
        UserId = userId; Action = action.Trim(); EntityType = entityType.Trim(); EntityId = entityId; Details = details;
    }

    public Guid? UserId { get; private set; }
    public string Action { get; private set; } = string.Empty;
    public string EntityType { get; private set; } = string.Empty;
    public Guid? EntityId { get; private set; }
    public string? Details { get; private set; }
}
