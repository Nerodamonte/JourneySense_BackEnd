using System.Security.Claims;
using JSEA_Application.Constants;
using JSEA_Application.DTOs.Journey;
using JSEA_Application.DTOs.Request.Journey;
using JSEA_Application.DTOs.Respone.Journey;
using JSEA_Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace JSEA_Presentation.Controllers;

/// <summary>
/// Toolkit: bv / xăng / sửa xe / thuốc → nearby trả 1 điểm + SignalR ngay khi journey đã start.
/// Quán ăn / nghỉ / cà phê → nearby trả list; SignalR <c>EmergencyPlaceSelected</c> khi user chọn chỗ (POST announce).
/// </summary>
[ApiController]
[Route("api/emergency")]
public class EmergencyController : ControllerBase
{
    private readonly IEmergencyNearbyService _emergencyNearbyService;
    private readonly IJourneyService _journeyService;
    private readonly IJourneyLiveNotifier _journeyLiveNotifier;
    private readonly IUserProfileRepository _userProfileRepository;
    private readonly IJourneyMemberRepository _journeyMemberRepository;
    private readonly IGoongMapsService _goongMapsService;

    public EmergencyController(
        IEmergencyNearbyService emergencyNearbyService,
        IJourneyService journeyService,
        IJourneyLiveNotifier journeyLiveNotifier,
        IUserProfileRepository userProfileRepository,
        IJourneyMemberRepository journeyMemberRepository,
        IGoongMapsService goongMapsService)
    {
        _emergencyNearbyService = emergencyNearbyService;
        _journeyService = journeyService;
        _journeyLiveNotifier = journeyLiveNotifier;
        _userProfileRepository = userProfileRepository;
        _journeyMemberRepository = journeyMemberRepository;
        _goongMapsService = goongMapsService;
    }

    /// <summary>
    /// Một lần gọi: tìm điểm gần nhất + polyline. JWT hoặc (có <c>journeyId</c> thì bắt buộc <c>guestKey</c> hợp lệ).
    /// Không journey: chỉ user đăng nhập (tránh lạm dụng API Goong).
    /// </summary>
    [HttpPost("nearby")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(List<EmergencyNearbyItemResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Nearby([FromBody] EmergencyNearbyRequest request, CancellationToken cancellationToken)
    {
        if (request == null || !ModelState.IsValid)
            return BadRequest(ModelState);

        Guid? travelerId = null;
        if (User?.Identity?.IsAuthenticated == true &&
            Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var tid))
            travelerId = tid;

        if (!travelerId.HasValue && !request.JourneyId.HasValue)
            return Unauthorized(new { message = "Cần đăng nhập, hoặc gửi kèm journeyId + guestKey." });

        if (request.JourneyId.HasValue)
        {
            if (travelerId.HasValue)
            {
                try
                {
                    await _journeyService.VerifyTravelerCanNavigateJourneyAsync(
                        request.JourneyId.Value,
                        travelerId.Value,
                        cancellationToken);
                }
                catch (KeyNotFoundException)
                {
                    return NotFound(new { message = "Không tìm thấy hành trình." });
                }
                catch (UnauthorizedAccessException)
                {
                    return NotFound(new { message = "Không tìm thấy hành trình." });
                }
            }
            else
            {
                if (!request.GuestKey.HasValue)
                    return Unauthorized(new { message = "Khách cần guestKey để gắn với hành trình." });

                try
                {
                    await _journeyService.VerifyGuestCanNavigateJourneyAsync(
                        request.JourneyId.Value,
                        request.GuestKey.Value,
                        cancellationToken);
                }
                catch (KeyNotFoundException)
                {
                    return NotFound(new { message = "Không tìm thấy hành trình." });
                }
                catch (UnauthorizedAccessException)
                {
                    return NotFound(new { message = "Không tìm thấy hành trình." });
                }
            }
        }

        var (status, message, items) = await _emergencyNearbyService.GetNearbyAsync(request, cancellationToken);
        if (status != 200)
            return BadRequest(new { message = message });

        var shouldNotifyGroup = items.Count > 0
            && EmergencyPlaceTypes.PrefersSingleRoute(items[0].Type);

        if (request.JourneyId.HasValue && shouldNotifyGroup)
        {
            try
            {
                if (travelerId.HasValue)
                {
                    await _journeyService.VerifyTravelerCanNavigateStartedJourneyAsync(
                        request.JourneyId.Value,
                        travelerId.Value,
                        cancellationToken);

                    var first = items[0];
                    var displayName = (await _userProfileRepository.GetByUserIdAsync(travelerId.Value, cancellationToken))?.FullName?.Trim()
                        ?? "Thành viên";

                    await _journeyLiveNotifier.NotifyEmergencyPlaceSelectedAsync(
                        new JourneyEmergencySelectionNotification
                        {
                            JourneyId = request.JourneyId.Value,
                            AnnouncedByTravelerId = travelerId.Value,
                            AnnouncedByGuestKey = null,
                            AnnouncedByDisplayName = displayName,
                            Type = first.Type,
                            PlaceId = first.PlaceId,
                            Name = first.Name,
                            FormattedAddress = first.FormattedAddress,
                            Latitude = first.Latitude,
                            Longitude = first.Longitude,
                            AtUtc = DateTime.UtcNow
                        },
                        cancellationToken);
                }
                else if (request.GuestKey.HasValue)
                {
                    await _journeyService.VerifyGuestCanNavigateStartedJourneyAsync(
                        request.JourneyId.Value,
                        request.GuestKey.Value,
                        cancellationToken);

                    var first = items[0];
                    var guestKey = request.GuestKey.Value;
                    var member = await _journeyMemberRepository.GetActiveByGuestKeyAsync(
                        request.JourneyId.Value,
                        guestKey,
                        cancellationToken);
                    var displayName = member?.DisplayName?.Trim() ?? "Khách";

                    await _journeyLiveNotifier.NotifyEmergencyPlaceSelectedAsync(
                        new JourneyEmergencySelectionNotification
                        {
                            JourneyId = request.JourneyId.Value,
                            AnnouncedByTravelerId = Guid.Empty,
                            AnnouncedByGuestKey = guestKey,
                            AnnouncedByDisplayName = displayName,
                            Type = first.Type,
                            PlaceId = first.PlaceId,
                            Name = first.Name,
                            FormattedAddress = first.FormattedAddress,
                            Latitude = first.Latitude,
                            Longitude = first.Longitude,
                            AtUtc = DateTime.UtcNow
                        },
                        cancellationToken);
                }
            }
            catch (InvalidOperationException)
            {
                // Chưa start — vẫn trả kết quả tìm đường, không broadcast.
            }
            catch (KeyNotFoundException)
            {
            }
            catch (UnauthorizedAccessException)
            {
            }
        }

        return Ok(items);
    }

    /// <summary>
    /// Sau khi user chọn 1 địa điểm từ list (không phải loại khẩn cấp): lấy lại chi tiết Goong và broadcast <c>EmergencyPlaceSelected</c>.
    /// Bắt buộc có <c>journeyId</c>; journey phải đã start. JWT hoặc <c>guestKey</c>.
    /// </summary>
    [HttpPost("announce")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AnnounceSelectedPlace(
        [FromBody] EmergencyPlaceAnnounceRequest request,
        CancellationToken cancellationToken)
    {
        if (request == null || !ModelState.IsValid)
            return BadRequest(ModelState);

        var placeType = EmergencyPlaceTypes.NormalizeOrNull(request.Type);
        if (placeType == null)
            return BadRequest(new { message = "type không hợp lệ." });

        if (EmergencyPlaceTypes.PrefersSingleRoute(placeType))
            return BadRequest(new { message = "Loại khẩn cấp đã broadcast qua POST nearby; không dùng announce." });

        Guid? travelerId = null;
        if (User?.Identity?.IsAuthenticated == true &&
            Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var tid))
            travelerId = tid;

        if (!travelerId.HasValue && !request.GuestKey.HasValue)
            return Unauthorized(new { message = "Cần đăng nhập hoặc guestKey." });

        if (travelerId.HasValue)
        {
            try
            {
                await _journeyService.VerifyTravelerCanNavigateJourneyAsync(
                    request.JourneyId,
                    travelerId.Value,
                    cancellationToken);
            }
            catch (KeyNotFoundException)
            {
                return NotFound(new { message = "Không tìm thấy hành trình." });
            }
            catch (UnauthorizedAccessException)
            {
                return NotFound(new { message = "Không tìm thấy hành trình." });
            }
        }
        else
        {
            try
            {
                await _journeyService.VerifyGuestCanNavigateJourneyAsync(
                    request.JourneyId,
                    request.GuestKey!.Value,
                    cancellationToken);
            }
            catch (KeyNotFoundException)
            {
                return NotFound(new { message = "Không tìm thấy hành trình." });
            }
            catch (UnauthorizedAccessException)
            {
                return NotFound(new { message = "Không tìm thấy hành trình." });
            }
        }

        try
        {
            if (travelerId.HasValue)
                await _journeyService.VerifyTravelerCanNavigateStartedJourneyAsync(
                    request.JourneyId,
                    travelerId.Value,
                    cancellationToken);
            else
                await _journeyService.VerifyGuestCanNavigateStartedJourneyAsync(
                    request.JourneyId,
                    request.GuestKey!.Value,
                    cancellationToken);
        }
        catch (InvalidOperationException)
        {
            return BadRequest(new { message = "Hành trình chưa bắt đầu, không thể thông báo nhóm." });
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { message = "Không tìm thấy hành trình." });
        }
        catch (UnauthorizedAccessException)
        {
            return NotFound(new { message = "Không tìm thấy hành trình." });
        }

        var detail = await _goongMapsService.GetPlaceDetailAsync(request.PlaceId.Trim(), cancellationToken);
        if (detail?.Latitude == null || detail.Longitude == null)
            return NotFound(new { message = "Không tìm thấy địa điểm." });

        if (travelerId.HasValue)
        {
            var displayName = (await _userProfileRepository.GetByUserIdAsync(travelerId.Value, cancellationToken))?.FullName?.Trim()
                ?? "Thành viên";

            await _journeyLiveNotifier.NotifyEmergencyPlaceSelectedAsync(
                new JourneyEmergencySelectionNotification
                {
                    JourneyId = request.JourneyId,
                    AnnouncedByTravelerId = travelerId.Value,
                    AnnouncedByGuestKey = null,
                    AnnouncedByDisplayName = displayName,
                    Type = placeType,
                    PlaceId = detail.PlaceId,
                    Name = detail.Name,
                    FormattedAddress = detail.FormattedAddress,
                    Latitude = detail.Latitude,
                    Longitude = detail.Longitude,
                    AtUtc = DateTime.UtcNow
                },
                cancellationToken);
        }
        else
        {
            var guestKey = request.GuestKey!.Value;
            var member = await _journeyMemberRepository.GetActiveByGuestKeyAsync(
                request.JourneyId,
                guestKey,
                cancellationToken);
            var displayName = member?.DisplayName?.Trim() ?? "Khách";

            await _journeyLiveNotifier.NotifyEmergencyPlaceSelectedAsync(
                new JourneyEmergencySelectionNotification
                {
                    JourneyId = request.JourneyId,
                    AnnouncedByTravelerId = Guid.Empty,
                    AnnouncedByGuestKey = guestKey,
                    AnnouncedByDisplayName = displayName,
                    Type = placeType,
                    PlaceId = detail.PlaceId,
                    Name = detail.Name,
                    FormattedAddress = detail.FormattedAddress,
                    Latitude = detail.Latitude,
                    Longitude = detail.Longitude,
                    AtUtc = DateTime.UtcNow
                },
                cancellationToken);
        }

        return NoContent();
    }
}
