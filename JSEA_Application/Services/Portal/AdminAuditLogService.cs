using JSEA_Application.DTOs.Portal;
using JSEA_Application.Enums;
using JSEA_Application.Interfaces;

namespace JSEA_Application.Services.Portal;

public class AdminAuditLogService : IAdminAuditLogService
{
    private readonly IAuditLogRepository _auditLogs;

    public AdminAuditLogService(IAuditLogRepository auditLogs)
    {
        _auditLogs = auditLogs;
    }

    public async Task<PortalPagedResult<AuditLogListItemDto>> ListAsync(
        Guid? actorUserId,
        ActionType? actionType,
        string? entityType,
        DateTime? fromUtc,
        DateTime? toUtc,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var (items, total) = await _auditLogs.GetPagedAsync(
            actorUserId,
            actionType,
            entityType,
            fromUtc,
            toUtc,
            page,
            pageSize,
            cancellationToken);

        return new PortalPagedResult<AuditLogListItemDto>
        {
            Page = Math.Max(1, page),
            PageSize = Math.Clamp(pageSize, 1, 200),
            TotalCount = total,
            Items = items.Select(a => new AuditLogListItemDto
            {
                Id = a.Id,
                ActorUserId = a.UserId,
                ActorEmail = a.User?.Email,
                ActionType = a.ActionType,
                EntityType = a.EntityType,
                EntityId = a.EntityId,
                OldValues = a.OldValues,
                NewValues = a.NewValues,
                CreatedAt = a.CreatedAt
            }).ToList()
        };
    }
}
