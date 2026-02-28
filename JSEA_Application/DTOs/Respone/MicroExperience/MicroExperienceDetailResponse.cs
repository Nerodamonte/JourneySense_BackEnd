using JSEA_Application.Enums;

namespace JSEA_Application.DTOs.Respone.MicroExperience;

public class MicroExperienceDetailResponse
{
    public Guid Id { get; set; }
    public string? Name { get; set; }
    public string? CategoryName { get; set; }
    public string? Description { get; set; }
    public decimal AvgRating { get; set; }
    public ExperienceStatus? Status { get; set; }
    public string? Address { get; set; }
    public string? City { get; set; }
    public string? Country { get; set; }
}
