using JSEA_Application.Constants;
using JSEA_Application.DTOs.Portal;
using JSEA_Application.Enums;
using JSEA_Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace JSEA_Presentation.Controllers;

[ApiController]
[Route("api/admin/audit-logs")]
[Authorize(Roles = AppRoles.Admin)]
public class AdminAuditLogsController : ControllerBase
{
    private readonly IAdminAuditLogService _auditLogs;

    public AdminAuditLogsController(IAdminAuditLogService auditLogs)
    {
        _auditLogs = auditLogs;
    }

    /// <summary>6) Xem nhật ký thao tác (phân trang, lọc).</summary>
    [HttpGet]
    [ProducesResponseType(typeof(PortalPagedResult<AuditLogListItemDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> List(
        [FromQuery] Guid? userId,
        [FromQuery] string? actionType,
        [FromQuery] string? entityType,
        [FromQuery] DateTime? fromUtc,
        [FromQuery] DateTime? toUtc,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 30,
        CancellationToken cancellationToken = default)
    {
        ActionType? at = null;
        if (!string.IsNullOrWhiteSpace(actionType) &&
            Enum.TryParse<ActionType>(actionType.Trim(), ignoreCase: true, out var parsed))
            at = parsed;

        var result = await _auditLogs.ListAsync(userId, at, entityType, fromUtc, toUtc, page, pageSize, cancellationToken);
        return Ok(result);
    }
}
