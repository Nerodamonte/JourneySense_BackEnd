using JSEA_Application.DTOs.Portal;
using System.Net;

namespace JSEA_Application.Interfaces;

public interface IAdminUserService
{
    Task<PortalPagedResult<AdminUserListItemDto>> ListUsersAsync(
        string? role,
        string? status,
        string? search,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<AdminUserDetailDto?> GetUserByIdAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<(bool Ok, string? Error)> UpdateUserStatusAsync(
        Guid actorUserId,
        Guid targetUserId,
        UpdatePortalUserStatusRequest request,
        IPAddress? ip,
        string? userAgent,
        CancellationToken cancellationToken = default);

    Task<(AdminUserDetailDto? User, string? Error)> CreateStaffAccountAsync(
        Guid actorUserId,
        CreateStaffAccountRequest request,
        IPAddress? ip,
        string? userAgent,
        CancellationToken cancellationToken = default);
}
