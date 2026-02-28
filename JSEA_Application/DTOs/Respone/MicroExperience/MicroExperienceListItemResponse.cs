using JSEA_Application.Enums;

namespace JSEA_Application.DTOs.Respone.MicroExperience;

public class MicroExperienceListItemResponse
{
    public Guid Id { get; set; }
    public string? Name { get; set; }
    public string? City { get; set; }
    public ExperienceStatus? Status { get; set; }
}
