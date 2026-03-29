using JSEA_Application.Enums;
using System.Net;

namespace JSEA_Application.Interfaces;

public interface IPortalAuditLogger
{
    Task LogAsync(
        Guid? actorUserId,
        ActionType actionType,
        string? entityType,
        Guid? entityId,
        object? oldPayload,
        object? newPayload,
        IPAddress? ipAddress,
        string? userAgent,
        CancellationToken cancellationToken = default);
}
