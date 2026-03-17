using System.ComponentModel.DataAnnotations;
using JSEA_Application.Enums;

namespace JSEA_Application.DTOs.Request.Journey;

/// <summary>
/// Request để thiết lập một hành trình mới (journey) từ điểm A → B.
/// </summary>
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

    /// <summary>Mức độ đông đúc ưu tiên (All, Quiet, Normal, Busy).</summary>
    public CrowdLevel PreferredCrowdLevel { get; set; } = CrowdLevel.All;

    [Required]
    [Range(1, 1440, ErrorMessage = "Thời gian dự kiến phải từ 1 đến 1440 phút")]
    public int TimeBudgetMinutes { get; set; }

    /// <summary>Khoảng cách detour tối đa cho các gợi ý Experience (mét).</summary>
    [Range(0, 100_000)]
    public int MaxDetourDistanceMeters { get; set; }

    /// <summary>Số điểm dừng (micro-experience) tối đa mong muốn dọc tuyến (0–20).</summary>
    [Range(0, 20)]
    public int MaxStopCount { get; set; }

    /// <summary>Địa chỉ các điểm dừng tùy chọn giữa xuất phát và đến (thứ tự từ origin → destination).</summary>
    public List<string>? WaypointAddresses { get; set; }
}
