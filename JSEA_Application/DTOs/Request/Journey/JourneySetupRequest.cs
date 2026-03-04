using System.ComponentModel.DataAnnotations;
using JSEA_Application.Enums;

namespace JSEA_Application.DTOs.Request.Journey;

public class JourneySetupRequest
{
    [Required(ErrorMessage = "Địa chỉ xuất phát không được để trống")]
    [StringLength(500)]
    public string OriginAddress { get; set; } = null!;

    [Required(ErrorMessage = "Địa chỉ đến không được để trống")]
    [StringLength(500)]
    public string DestinationAddress { get; set; } = null!;

    public VehicleType VehicleType { get; set; }

    /// <summary>Travel vibe / mood cho hành trình (Relax, Photography, Foodie, Adventure, Culture).</summary>
    public MoodType? CurrentMood { get; set; }

    [Required]
    [Range(1, 1440, ErrorMessage = "Thời gian dự kiến phải từ 1 đến 1440 phút")]
    public int TimeBudgetMinutes { get; set; }

    [Range(0, 100_000)]
    public int MaxDetourDistanceMeters { get; set; }

    /// <summary>Số điểm dừng (micro-experience) tối đa mong muốn dọc tuyến (0–20).</summary>
    [Range(0, 20)]
    public int MaxStopCount { get; set; }

    /// <summary>Thời gian dừng ưu tiên mỗi điểm (phút, 5–60).</summary>
    [Range(5, 60)]
    public int? PreferredStopDurationMinutes { get; set; }

    /// <summary>Địa chỉ các điểm dừng tùy chọn giữa xuất phát và đến (thứ tự từ origin → destination).</summary>
    public List<string>? WaypointAddresses { get; set; }
}
