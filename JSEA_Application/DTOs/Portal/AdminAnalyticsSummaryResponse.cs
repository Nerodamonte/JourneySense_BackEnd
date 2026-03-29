namespace JSEA_Application.DTOs.Portal;

public class AdminAnalyticsSummaryResponse
{
    public int UsersTotal { get; set; }
    public int UsersActive { get; set; }
    public int UsersTraveler { get; set; }
    public int UsersStaff { get; set; }
    public int UsersAdmin { get; set; }
    public int ExperiencesActive { get; set; }
    public int JourneysTotal { get; set; }
    public int FeedbacksPendingModeration { get; set; }
}
