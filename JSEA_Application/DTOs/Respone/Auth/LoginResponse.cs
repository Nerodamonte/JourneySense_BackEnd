namespace JSEA_Application.DTOs.Respone.Auth;

public class LoginResponse
{
    public Guid UserId { get; set; }
    public string Email { get; set; } = null!;
    public string Role { get; set; } = null!;
    public string AccessToken { get; set; } = null!;
    public string RefreshToken { get; set; } = null!;
}
