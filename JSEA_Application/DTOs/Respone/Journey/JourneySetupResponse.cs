using JSEA_Application.Enums;

namespace JSEA_Application.DTOs.Respone.Journey;

/// <summary>
/// Kết quả sau khi thiết lập journey: id hành trình + tóm tắt tuyến + danh sách routes từ Goong.
/// </summary>
public class JourneySetupResponse
{
    /// <summary>Id journey vừa được tạo.</summary>
    public Guid JourneyId { get; set; }

    /// <summary>Trạng thái hiện tại (mặc định Planning).</summary>
    public JourneyStatus Status { get; set; }

    /// <summary>Tóm tắt tuyến (quãng đường, thời gian) để hiển thị cho user.</summary>
    public string Summary { get; set; } = null!;

    /// <summary>Địa chỉ xuất phát.</summary>
    public string? OriginAddress { get; set; }

    /// <summary>Địa chỉ đến.</summary>
    public string? DestinationAddress { get; set; }

    /// <summary>Phương tiện di chuyển.</summary>
    public VehicleType? VehicleType { get; set; }

    /// <summary>Time budget tổng (phút).</summary>
    public int? TimeBudgetMinutes { get; set; }

    /// <summary>Khoảng cách detour tối đa cho các gợi ý Experience (mét).</summary>
    public int? MaxDetourDistanceMeters { get; set; }

    /// <summary>Mood/vibe được chọn (nếu có).</summary>
    public MoodType? CurrentMood { get; set; }

    /// <summary>Số điểm dừng tối đa mong muốn.</summary>
    public int? MaxStopCount { get; set; }

    /// <summary>Các tuyến đường mà Goong Maps trả về (tối đa MaxRouteAlternatives). Phần tử đầu là tuyến đã được tạo journey.</summary>
    public List<RouteContext> Routes { get; set; } = new();
}
