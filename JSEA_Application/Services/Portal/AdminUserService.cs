using JSEA_Application.Constants;
using JSEA_Application.DTOs.Portal;
using JSEA_Application.Enums;
using JSEA_Application.Interfaces;
using JSEA_Application.Models;
using System.Net;

namespace JSEA_Application.Services.Portal;

public class AdminUserService : IAdminUserService
{
    private readonly IUserRepository _users;
    private readonly IPortalAuditLogger _audit;

    public AdminUserService(IUserRepository users, IPortalAuditLogger audit)
    {
        _users = users;
        _audit = audit;
    }

    public async Task<PortalPagedResult<AdminUserListItemDto>> ListUsersAsync(
        string? role,
        string? status,
        string? search,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var (items, total) = await _users.GetPagedAsync(role, status, search, page, pageSize, cancellationToken);
        return new PortalPagedResult<AdminUserListItemDto>
        {
            Page = Math.Max(1, page),
            PageSize = Math.Clamp(pageSize, 1, 100),
            TotalCount = total,
            Items = items.Select(u => new AdminUserListItemDto
            {
                Id = u.Id,
                Email = u.Email,
                Phone = u.Phone,
                Role = u.Role,
                Status = u.Status,
                CreatedAt = u.CreatedAt,
                LastLoginAt = u.LastLoginAt
            }).ToList()
        };
    }

    public async Task<AdminUserDetailDto?> GetUserByIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var u = await _users.GetByIdAsync(userId);
        if (u == null || u.DeletedAt != null)
            return null;

        return new AdminUserDetailDto
        {
            Id = u.Id,
            Email = u.Email,
            Phone = u.Phone,
            Role = u.Role,
            Status = u.Status,
            EmailVerified = u.EmailVerified,
            PhoneVerified = u.PhoneVerified,
            CreatedAt = u.CreatedAt,
            UpdatedAt = u.UpdatedAt,
            LastLoginAt = u.LastLoginAt
        };
    }

    public async Task<(bool Ok, string? Error)> UpdateUserStatusAsync(
        Guid actorUserId,
        Guid targetUserId,
        UpdatePortalUserStatusRequest request,
        IPAddress? ip,
        string? userAgent,
        CancellationToken cancellationToken = default)
    {
        var status = (request.Status ?? "").Trim().ToLowerInvariant();
        if (status is not ("active" or "suspended"))
            return (false, "Status chỉ hỗ trợ active hoặc suspended.");

        var target = await _users.GetByIdAsync(targetUserId);
        if (target == null || target.DeletedAt != null)
            return (false, "Không tìm thấy user.");

        if (string.Equals(target.Role, AppRoles.Admin, StringComparison.OrdinalIgnoreCase))
            return (false, "Không thể đổi trạng thái tài khoản admin.");

        var old = new { target.Status };
        target.Status = status;
        target.UpdatedAt = DateTime.UtcNow;
        await _users.UpdateAsync(target);

        await _audit.LogAsync(
            actorUserId,
            ActionType.AdminUserStatusChanged,
            nameof(User),
            targetUserId,
            old,
            new { status, request.Reason },
            ip,
            userAgent,
            cancellationToken);

        return (true, null);
    }

    public async Task<(AdminUserDetailDto? User, string? Error)> CreateStaffAccountAsync(
        Guid actorUserId,
        CreateStaffAccountRequest request,
        IPAddress? ip,
        string? userAgent,
        CancellationToken cancellationToken = default)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        if (await _users.GetByEmailAsync(email) != null)
            return (null, "Email đã tồn tại.");

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = email,
            Role = AppRoles.Staff,
            Status = "active",
            EmailVerified = true,
            CreatedAt = DateTime.UtcNow,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password)
        };

        await _users.CreateAsync(user);

        await _audit.LogAsync(
            actorUserId,
            ActionType.AdminStaffCreated,
            nameof(User),
            user.Id,
            null,
            new { user.Email, user.Role },
            ip,
            userAgent,
            cancellationToken);

        var dto = await GetUserByIdAsync(user.Id, cancellationToken);
        return (dto, null);
    }
}
