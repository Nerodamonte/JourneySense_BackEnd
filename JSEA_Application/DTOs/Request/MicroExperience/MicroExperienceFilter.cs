using JSEA_Application.Enums;

namespace JSEA_Application.DTOs.Request.MicroExperience;

public class MicroExperienceFilter
{
    public string? Keyword { get; set; }
    public Guid? CategoryId { get; set; }
    public ExperienceStatus? Status { get; set; }
}
