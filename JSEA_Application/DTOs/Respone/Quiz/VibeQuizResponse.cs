using JSEA_Application.Enums;

namespace JSEA_Application.DTOs.Respone.Quiz;

public class VibeQuizResponse
{
    public string QuizId { get; set; } = "vibe-v1";
    public List<VibeQuizQuestionResponse> Questions { get; set; } = new();
}

public class VibeQuizQuestionResponse
{
    public string Id { get; set; } = null!;
    public string Text { get; set; } = null!;
    public bool MultiSelect { get; set; }
    public int MinSelect { get; set; } = 1;
    public int MaxSelect { get; set; } = 1;
    public List<VibeQuizOptionResponse> Options { get; set; } = new();
}

public class VibeQuizOptionResponse
{
    public string Id { get; set; } = null!;
    public string Text { get; set; } = null!;

    // Weight map for transparency/debug; FE doesn't need to interpret it.
    public Dictionary<VibeType, int> Weights { get; set; } = new();
}
