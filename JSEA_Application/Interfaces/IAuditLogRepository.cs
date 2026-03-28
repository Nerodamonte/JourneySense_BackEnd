using JSEA_Application.Enums;
using JSEA_Application.Models;

namespace JSEA_Application.Interfaces;

public interface IAuditLogRepository
{
    Task AddAsync(AuditLog log, CancellationToken cancellationToken = default);

    Task<(List<AuditLog> Items, int TotalCount)> GetPagedAsync(
        Guid? actorUserId,
        ActionType? actionType,
        string? entityType,
        DateTime? fromUtc,
        DateTime? toUtc,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);
}
