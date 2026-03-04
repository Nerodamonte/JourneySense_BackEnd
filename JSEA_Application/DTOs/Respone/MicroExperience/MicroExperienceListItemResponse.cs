namespace JSEA_Application.DTOs.Respone.MicroExperience;

public class MicroExperienceListItemResponse
{
    public Guid Id { get; set; }
    public string? Name { get; set; }
    public string? City { get; set; }
    public string? Status { get; set; }
    public List<string>? PreferredTimes { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
}
