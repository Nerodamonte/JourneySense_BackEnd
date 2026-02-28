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

    [Required]
    [Range(1, 1440, ErrorMessage = "Thời gian dự kiến phải từ 1 đến 1440 phút")]
    public int TimeBudgetMinutes { get; set; }

    [Range(0, 100_000)]
    public int MaxDetourDistanceMeters { get; set; }
}
