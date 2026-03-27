using JSEA_Application.Enums;

namespace JSEA_Application.DTOs.Request.Journey;

public class UpdateJourneyMoodRequest
{
    public MoodType? CurrentMood { get; set; }
}
