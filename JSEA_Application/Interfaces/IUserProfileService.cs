using JSEA_Application.DTOs.Request.Profile;
using JSEA_Application.DTOs.Respone.Profile;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JSEA_Application.Interfaces
{
    public interface IUserProfileService
    {
        /// <summary>
        /// Cập nhật profile của user (full_name, avatar, bio, accessibility_needs, travel_style).
        /// Tự động generate travel_style_text bằng Gemini từ travel_style array rồi lưu vào DB.
        /// </summary>
        Task UpdateProfileAsync(Guid userId, UpdateProfileRequest request, CancellationToken cancellationToken = default);

        Task<ProfileResponse> GetProfileAsync(Guid userId, CancellationToken cancellationToken = default);
    }
}
