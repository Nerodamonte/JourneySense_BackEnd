using System.ComponentModel.DataAnnotations;
using JSEA_Application.Enums;

namespace JSEA_Application.DTOs.Request.Journey;

/// <summary>
/// Request để thiết lập một hành trình mới (journey) từ điểm A → B.
/// </summary>
public class JourneySetupRequest : IValidatableObject
{
    /// <summary>
    /// Tọa độ điểm xuất phát (vĩ độ). Nếu dùng tọa độ thì phải có đủ OriginLatitude + OriginLongitude.
    /// </summary>
    public double? OriginLatitude { get; set; }

    /// <summary>
    /// Tọa độ điểm xuất phát (kinh độ). Nếu dùng tọa độ thì phải có đủ OriginLatitude + OriginLongitude.
    /// </summary>
    public double? OriginLongitude { get; set; }

    /// <summary>
    /// Địa chỉ hiển thị (optional). FE có thể lấy từ place-detail (formatted_address) để lưu/hiển thị.
    /// </summary>
    [StringLength(500)]
    public string? OriginAddress { get; set; }

    /// <summary>
    /// Tọa độ điểm đến (vĩ độ). Nếu dùng tọa độ thì phải có đủ DestinationLatitude + DestinationLongitude.
    /// </summary>
    public double? DestinationLatitude { get; set; }

    /// <summary>
    /// Tọa độ điểm đến (kinh độ). Nếu dùng tọa độ thì phải có đủ DestinationLatitude + DestinationLongitude.
    /// </summary>
    public double? DestinationLongitude { get; set; }

    /// <summary>
    /// Địa chỉ hiển thị (optional). FE có thể lấy từ place-detail (formatted_address) để lưu/hiển thị.
    /// </summary>
    [StringLength(500)]
    public string? DestinationAddress { get; set; }

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

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        var hasOriginCoords = OriginLatitude.HasValue || OriginLongitude.HasValue;
        var hasDestCoords = DestinationLatitude.HasValue || DestinationLongitude.HasValue;

        if (hasOriginCoords)
        {
            if (!OriginLatitude.HasValue || !OriginLongitude.HasValue)
                yield return new ValidationResult(
                    "OriginLatitude và OriginLongitude phải có đủ.",
                    new[] { nameof(OriginLatitude), nameof(OriginLongitude) });
            else
            {
                if (OriginLatitude.Value is < -90 or > 90)
                    yield return new ValidationResult(
                        "OriginLatitude phải nằm trong [-90, 90].",
                        new[] { nameof(OriginLatitude) });
                if (OriginLongitude.Value is < -180 or > 180)
                    yield return new ValidationResult(
                        "OriginLongitude phải nằm trong [-180, 180].",
                        new[] { nameof(OriginLongitude) });
            }
        }
        else if (string.IsNullOrWhiteSpace(OriginAddress))
        {
            yield return new ValidationResult(
                "Cần cung cấp OriginLatitude/OriginLongitude hoặc OriginAddress.",
                new[] { nameof(OriginLatitude), nameof(OriginLongitude), nameof(OriginAddress) });
        }

        if (hasDestCoords)
        {
            if (!DestinationLatitude.HasValue || !DestinationLongitude.HasValue)
                yield return new ValidationResult(
                    "DestinationLatitude và DestinationLongitude phải có đủ.",
                    new[] { nameof(DestinationLatitude), nameof(DestinationLongitude) });
            else
            {
                if (DestinationLatitude.Value is < -90 or > 90)
                    yield return new ValidationResult(
                        "DestinationLatitude phải nằm trong [-90, 90].",
                        new[] { nameof(DestinationLatitude) });
                if (DestinationLongitude.Value is < -180 or > 180)
                    yield return new ValidationResult(
                        "DestinationLongitude phải nằm trong [-180, 180].",
                        new[] { nameof(DestinationLongitude) });
            }
        }
        else if (string.IsNullOrWhiteSpace(DestinationAddress))
        {
            yield return new ValidationResult(
                "Cần cung cấp DestinationLatitude/DestinationLongitude hoặc DestinationAddress.",
                new[] { nameof(DestinationLatitude), nameof(DestinationLongitude), nameof(DestinationAddress) });
        }
    }
}
