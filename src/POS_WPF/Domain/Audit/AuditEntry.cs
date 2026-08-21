using POS_WPF.Domain.Common;

namespace POS_WPF.Domain.Audit;

public sealed class AuditEntry : Entity
{
    private AuditEntry() { }
    public AuditEntry(Guid? userId, string action, string entityType, Guid? entityId, string? oldValue, string? newValue, string? reason, Guid? terminalId, string? details)
    {
        UserId = userId; Action = action.Trim(); EntityType = entityType.Trim(); EntityId = entityId; OldValue = oldValue; NewValue = newValue; Reason = reason; TerminalId = terminalId; Details = details;
    }
    public Guid? UserId { get; private set; }
    public Guid? TerminalId { get; private set; }
    public string Action { get; private set; } = string.Empty;
    public string EntityType { get; private set; } = string.Empty;
    public Guid? EntityId { get; private set; }
    public string? OldValue { get; private set; }
    public string? NewValue { get; private set; }
    public string? Reason { get; private set; }
    public string? Details { get; private set; }
}
