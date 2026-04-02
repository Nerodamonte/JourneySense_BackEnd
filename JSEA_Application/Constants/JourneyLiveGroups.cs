namespace JSEA_Application.Constants;

public static class JourneyLiveGroups
{
    public static string ForJourney(Guid journeyId) => $"journey:{journeyId:D}";
}
