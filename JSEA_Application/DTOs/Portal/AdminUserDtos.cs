using System.ComponentModel.DataAnnotations;

namespace JSEA_Application.DTOs.Portal;

public class AdminUserListItemDto
{
    public Guid Id { get; set; }
    public string Email { get; set; } = null!;
    public string? Phone { get; set; }
    public string Role { get; set; } = null!;
    public string Status { get; set; } = null!;
    public DateTime? CreatedAt { get; set; }
    public DateTime? LastLoginAt { get; set; }

    /// <summary>Ảnh đại diện từ user_profiles.avatar_url (null nếu chưa có hồ sơ / chưa đặt).</summary>
    public string? AvatarUrl { get; set; }
}

public class AdminUserDetailDto
{
    public Guid Id { get; set; }
    public string Email { get; set; } = null!;
    public string? Phone { get; set; }
    public string Role { get; set; } = null!;
    public string Status { get; set; } = null!;
    public bool? EmailVerified { get; set; }
    public bool? PhoneVerified { get; set; }
    public DateTime? CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public DateTime? LastLoginAt { get; set; }

    /// <summary>Từ user_profiles — có thể null nếu chưa tạo hồ sơ.</summary>
    public string? FullName { get; set; }

    public string? AvatarUrl { get; set; }

    /// <summary>Cùng nguồn với avatarUrl — tiện cho FE đặt tên “ảnh đại diện”.</summary>
    public string? PhotoUrl { get; set; }

    public string? Bio { get; set; }
}

public class CreateStaffAccountRequest
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = null!;

    [Required]
    [MinLength(8, ErrorMessage = "Mật khẩu tối thiểu 8 ký tự")]
    public string Password { get; set; } = null!;
}

public class UpdatePortalUserStatusRequest
{
    /// <summary>active | suspended</summary>
    [Required]
    public string Status { get; set; } = null!;

    public string? Reason { get; set; }
}
