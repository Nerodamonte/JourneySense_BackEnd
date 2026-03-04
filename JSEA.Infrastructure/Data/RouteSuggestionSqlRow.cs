namespace JSEA_Infrastructure.Data;

/// <summary>Keyless row type for raw SQL result: experiences near route.</summary>
internal class RouteSuggestionSqlRow
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
