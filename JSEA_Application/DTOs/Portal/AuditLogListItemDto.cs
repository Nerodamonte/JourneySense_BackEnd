using JSEA_Application.Enums;

namespace JSEA_Application.DTOs.Portal;

public class AuditLogListItemDto
{
    public Guid Id { get; set; }
    public Guid? ActorUserId { get; set; }
    public string? ActorEmail { get; set; }
    public ActionType ActionType { get; set; }
    public string? EntityType { get; set; }
    public Guid? EntityId { get; set; }
    public string? OldValues { get; set; }
    public string? NewValues { get; set; }
    public DateTime? CreatedAt { get; set; }
}
