namespace JSEA_Application.DTOs.Request.MicroExperience;

public class MicroExperienceFilter
{
    public string? Keyword { get; set; }
    public Guid? CategoryId { get; set; }
    public string? Status { get; set; } // active | inactive

    /// <summary>Lọc theo mood (tên factor type=mood).</summary>
    public string? Mood { get; set; }

    /// <summary>Lọc theo khung giờ (Morning, Afternoon, Evening, Night).</summary>
    public string? TimeOfDay { get; set; }
}
