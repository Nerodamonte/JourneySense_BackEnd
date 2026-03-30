namespace JSEA_Application.DTOs.Respone.Auth;

public class LoginResponse
{
    public Guid UserId { get; set; }
    public string Email { get; set; } = null!;
    public string Role { get; set; } = null!;
    public string AccessToken { get; set; } = null!;
    public string RefreshToken { get; set; } = null!;

    /// <summary>True khi traveler chưa có travel style và chưa xong quiz — FE mở quiz; đã tự set style trên profile thì false.</summary>
    public bool RequiresVibeQuiz { get; set; }
}
