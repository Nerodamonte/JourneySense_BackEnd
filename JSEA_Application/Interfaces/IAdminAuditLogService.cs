using JSEA_Application.DTOs.Portal;
using JSEA_Application.Enums;

namespace JSEA_Application.Interfaces;

public interface IAdminAuditLogService
{
    Task<PortalPagedResult<AuditLogListItemDto>> ListAsync(
        Guid? actorUserId,
        ActionType? actionType,
        string? entityType,
        DateTime? fromUtc,
        DateTime? toUtc,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);
}
