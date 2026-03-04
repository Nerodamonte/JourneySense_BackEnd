using JSEA_Application.Enums;

namespace JSEA_Application.DTOs.Respone.Journey;

/// <summary>
/// Tóm tắt một hành trình của người dùng để hiển thị trong danh sách (history).
/// </summary>
public class JourneyListItemResponse
{
    /// <summary>Id của journey.</summary>
    public Guid Id { get; set; }

    /// <summary>Địa chỉ xuất phát mà người dùng nhập.</summary>
    public string? OriginAddress { get; set; }

    /// <summary>Địa chỉ đích mà người dùng nhập.</summary>
    public string? DestinationAddress { get; set; }

    /// <summary>Phương tiện di chuyển (Walking, Bicycle, Motorbike, Car...).</summary>
    public VehicleType? VehicleType { get; set; }

    /// <summary>Thời gian dự kiến tổng cho hành trình (phút), bao gồm di chuyển + dừng.</summary>
    public int? TimeBudgetMinutes { get; set; }

    /// <summary>Mood/vibe hiện tại (nếu có) gắn với journey.</summary>
    public MoodType? CurrentMood { get; set; }

    /// <summary>Trạng thái hành trình (Planning, InProgress, Completed, Cancelled ...).</summary>
    public JourneyStatus? Status { get; set; }

    /// <summary>Thời điểm tạo journey.</summary>
    public DateTime? CreatedAt { get; set; }
}
