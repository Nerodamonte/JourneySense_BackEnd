using JSEA_Application.Enums;

namespace JSEA_Application.DTOs.Respone.Quiz;

public class SubmitVibeQuizResponse
{
    public string QuizId { get; set; } = "vibe-v1";

    public List<VibeType> Vibes { get; set; } = new();

    // Optional: score breakdown for debugging.
    public Dictionary<VibeType, int> Scores { get; set; } = new();
}
