using System.ComponentModel.DataAnnotations;

namespace JSEA_Application.DTOs.Request.JourneyProgress;

public class WaypointCheckOutRequest
{
    /// <summary>Tuỳ chọn (1–5). Bỏ hoặc null = checkout không tạo/cập nhật rating.</summary>
    [Range(1, 5)]
    public int? RatingValue { get; set; }
}
