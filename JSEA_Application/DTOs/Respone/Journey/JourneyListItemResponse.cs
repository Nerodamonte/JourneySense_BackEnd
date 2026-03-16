using JSEA_Application.Enums;

namespace JSEA_Application.DTOs.Respone.Journey;

/// <summary>
/// Tóm tắt một hành trình của người dùng để hiển thị trong danh sách (history).
/// </summary>
public class JourneyListItemResponse
{
    public Guid Id { get; set; }

    public string? OriginAddress { get; set; }

    public string? DestinationAddress { get; set; }

    public VehicleType? VehicleType { get; set; }

    /// <summary>Thời gian dự kiến tổng cho hành trình (phút), bao gồm di chuyển + detour + dừng.</summary>
    public int? TimeBudgetMinutes { get; set; }

    /// <summary>Mood/vibe hiện tại (nếu có) gắn với journey.</summary>
    public MoodType? CurrentMood { get; set; }

    /// <summary>Trạng thái hành trình (Planning, InProgress, Completed, Cancelled ...).</summary>
    public JourneyStatus? Status { get; set; }

    public DateTime? CreatedAt { get; set; }
}
