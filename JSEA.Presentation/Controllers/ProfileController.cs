using JSEA_Application.DTOs.Request.Profile;
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
        /// Cập nhật profile user. TravelStyle bắt buộc chọn để generate travel_style_text
        /// phục vụ suggest pipeline. Các field khác có thể bỏ trống.
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

            await _userProfileService.UpdateProfileAsync(userId, request, cancellationToken);

            return Ok(new { message = "Cập nhật profile thành công." });
        }
    }
}
