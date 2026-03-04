namespace JSEA_Application.DTOs.Respone.MicroExperience;

public class MicroExperienceDetailResponse
{
    public Guid Id { get; set; }
    public string? Name { get; set; }
    public string? CategoryName { get; set; }
    public string? Description { get; set; }
    public decimal AvgRating { get; set; }
    public string? Status { get; set; }
    public string? Address { get; set; }
    public string? City { get; set; }
    public string? Country { get; set; }
    public List<string>? AccessibleBy { get; set; }
    public List<string>? PreferredTimes { get; set; }
    public List<string>? WeatherSuitability { get; set; }
    public List<string>? Seasonality { get; set; }
    /// <summary>Tên các factor (vibe/mood) được tag.</summary>
    public List<string>? FactorNames { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
}
