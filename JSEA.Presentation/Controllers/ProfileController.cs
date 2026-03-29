using JSEA_Application.DTOs.Request.Profile;
using JSEA_Application.DTOs.Respone.Profile;
using JSEA_Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace JSEA_Presentation.Controllers
{
    [ApiController]
    [Route("api/profile")]
    [Authorize]
    public class ProfileController : ControllerBase
    {
        private readonly IUserProfileService _userProfileService;

        public ProfileController(IUserProfileService userProfileService)
        {
            _userProfileService = userProfileService;
        }

        /// <summary>
        /// Cập nhật profile user.
        /// Traveler: TravelStyle optional khi đã có trong DB; lần đầu bắt buộc ít nhất 1 vibe để generate travel_style_text.
        /// Admin/Staff: TravelStyle trong body bị bỏ qua (portal không dùng).
        /// Các field khác có thể bỏ trống.
        /// </summary>
        [HttpPut]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> UpdateProfile(
            [FromBody] UpdateProfileRequest request,
            CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(userIdClaim, out var userId))
                return Unauthorized();

            try
            {
                await _userProfileService.UpdateProfileAsync(userId, request, cancellationToken);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (UnauthorizedAccessException)
            {
                return Unauthorized(new { message = "Vui lòng đăng nhập." });
            }

            return Ok(new { message = "Cập nhật profile thành công." });
        }

        /// <summary>
        /// Admin/Staff: JSON không gồm travelStyle và point (chỉ dành cho traveler).
        /// </summary>
        [HttpGet]
        [ProducesResponseType(typeof(ProfileResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> GetMyProfile(CancellationToken cancellationToken)
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
                return Unauthorized(new { message = "Vui lòng đăng nhập." });

            try
            {
                var profile = await _userProfileService.GetProfileAsync(userId, cancellationToken);
                return Ok(profile);
            }
            catch (UnauthorizedAccessException)
            {
                return Unauthorized(new { message = "Vui lòng đăng nhập." });
            }
        }
    }
}
