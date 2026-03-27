namespace JSEA_Application.DTOs.Request.Quiz;

public class SubmitVibeQuizRequest
{
    public string QuizId { get; set; } = "vibe-v1";

    // questionId -> selected optionIds
    public Dictionary<string, List<string>> Answers { get; set; } = new();

    // If true, backend writes result into user profile TravelStyle.
    public bool ApplyToProfile { get; set; } = true;

    // How many vibes to return (default 3, max 3).
    public int Top { get; set; } = 3;
}
