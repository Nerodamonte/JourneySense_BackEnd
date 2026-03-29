using System.Net;
using System.Text.Json;
using JSEA_Application.Enums;
using JSEA_Application.Interfaces;
using JSEA_Application.Models;

namespace JSEA_Infrastructure.Services;

public class PortalAuditLogger : IPortalAuditLogger
{
    private readonly IAuditLogRepository _auditLogs;

    public PortalAuditLogger(IAuditLogRepository auditLogs)
    {
        _auditLogs = auditLogs;
    }

    public async Task LogAsync(
        Guid? actorUserId,
        ActionType actionType,
        string? entityType,
        Guid? entityId,
        object? oldPayload,
        object? newPayload,
        IPAddress? ipAddress,
        string? userAgent,
        CancellationToken cancellationToken = default)
    {
        string? oldJson = oldPayload == null ? null : JsonSerializer.Serialize(oldPayload);
        string? newJson = newPayload == null ? null : JsonSerializer.Serialize(newPayload);

        await _auditLogs.AddAsync(new AuditLog
        {
            Id = Guid.NewGuid(),
            UserId = actorUserId,
            ActionType = actionType,
            EntityType = entityType,
            EntityId = entityId,
            OldValues = oldJson,
            NewValues = newJson,
            IpAddress = ipAddress,
            UserAgent = string.IsNullOrWhiteSpace(userAgent) ? null : userAgent[..Math.Min(userAgent.Length, 500)],
            CreatedAt = DateTime.UtcNow
        }, cancellationToken);
    }
}
