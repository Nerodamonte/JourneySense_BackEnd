namespace JSEA_Application.DTOs.Respone.MicroExperience;

public class ExperiencePhotoResponse
{
    public Guid Id { get; set; }
    public string PhotoUrl { get; set; } = null!;
    public string? ThumbnailUrl { get; set; }
    public string? Caption { get; set; }
    public bool IsCover { get; set; }
    public DateTime? UploadedAt { get; set; }
}
