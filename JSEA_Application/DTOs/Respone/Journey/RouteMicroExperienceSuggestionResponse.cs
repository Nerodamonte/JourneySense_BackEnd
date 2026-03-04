namespace JSEA_Application.DTOs.Respone.Journey;

/// <summary>Gợi ý một experience dọc/gần tuyến, kèm khoảng cách lệch và thời gian dừng ước tính.</summary>
public class RouteMicroExperienceSuggestionResponse
{
    public Guid Id { get; set; }
    public string? Name { get; set; }
    public string? CategoryName { get; set; }
    public string? Address { get; set; }
    public string? City { get; set; }
    public string? Country { get; set; }
    public List<string>? PreferredTimes { get; set; }
    public string? Status { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public int DetourDistanceMeters { get; set; }
    public int EstimatedStopMinutes { get; set; }
}
