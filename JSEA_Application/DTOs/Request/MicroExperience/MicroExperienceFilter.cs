namespace JSEA_Application.DTOs.Request.MicroExperience;

/// <summary>
/// Bộ lọc cho danh sách Experience (dùng ở API GET /api/micro-experiences).
/// </summary>
public class MicroExperienceFilter
{
    /// <summary>Từ khóa tìm kiếm theo tên/địa chỉ/thành phố.</summary>
    public string? Keyword { get; set; }

    /// <summary>Lọc theo CategoryId.</summary>
    public Guid? CategoryId { get; set; }

    /// <summary>Lọc theo trạng thái (active / inactive).</summary>
    public string? Status { get; set; }

    /// <summary>Lọc theo mood (tên factor type = 'mood').</summary>
    public string? Mood { get; set; }

    /// <summary>Lọc theo khung giờ (Morning, Afternoon, Evening, Night).</summary>
    public string? TimeOfDay { get; set; }
}
