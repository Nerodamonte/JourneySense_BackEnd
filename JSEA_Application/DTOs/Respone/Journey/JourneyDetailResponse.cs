using JSEA_Application.Enums;

namespace JSEA_Application.DTOs.Respone.Journey;

/// <summary>
/// Thông tin chi tiết một journey (để xem lại hoặc debug gợi ý).
/// </summary>
public class JourneyDetailResponse
{
    /// <summary>Id journey.</summary>
    public Guid Id { get; set; }

    /// <summary>Id traveler (user) sở hữu journey.</summary>
    public Guid? TravelerId { get; set; }

    /// <summary>Địa chỉ xuất phát.</summary>
    public string? OriginAddress { get; set; }

    /// <summary>Địa chỉ đích.</summary>
    public string? DestinationAddress { get; set; }

    /// <summary>Phương tiện di chuyển.</summary>
    public VehicleType? VehicleType { get; set; }

    /// <summary>Tổng quãng đường tuyến chính (mét) do Goong trả về.</summary>
    public int? TotalDistanceMeters { get; set; }

    /// <summary>Thời gian di chuyển ước tính (phút) từ Goong (không tính dừng).</summary>
    public int? EstimatedDurationMinutes { get; set; }

    /// <summary>Time budget tổng mà người dùng cấu hình (phút).</summary>
    public int? TimeBudgetMinutes { get; set; }

    /// <summary>Khoảng cách detour tối đa cho các gợi ý Experience (mét).</summary>
    public int? MaxDetourDistanceMeters { get; set; }

    /// <summary>Mood/vibe hiện tại của journey (nếu có).</summary>
    public MoodType? CurrentMood { get; set; }

    /// <summary>Trạng thái hành trình.</summary>
    public JourneyStatus? Status { get; set; }

    /// <summary>Thời điểm journey bắt đầu (nếu đã start).</summary>
    public DateTime? StartedAt { get; set; }

    /// <summary>Thời điểm journey hoàn thành.</summary>
    public DateTime? CompletedAt { get; set; }

    /// <summary>Thời điểm journey được tạo.</summary>
    public DateTime? CreatedAt { get; set; }
}
