using JSEA_Application.Interfaces;
using JSEA_Application.Constants;
using JSEA_Application.Interfaces.Auth;
using JSEA_Application.DTOs.Respone.Auth;
using Microsoft.Extensions.Configuration;
using JSEA_Application.Models;

namespace JSEA_Application.Services.Auth;

public class AuthService : IAuthService
{
    private readonly IUserRepository _userRepository;
    private readonly IUserProfileRepository _userProfileRepository;
    private readonly IEmailOtpRepository _emailOtpRepository;
    private readonly IJwtService _jwtService;
    private readonly IConfiguration _configuration;

    public AuthService(
        IUserRepository userRepository,
        IUserProfileRepository userProfileRepository,
        IEmailOtpRepository emailOtpRepository,
        IJwtService jwtService,
        IConfiguration configuration)
    {
        _userRepository = userRepository;
        _userProfileRepository = userProfileRepository;
        _emailOtpRepository = emailOtpRepository;
        _jwtService = jwtService;
        _configuration = configuration;
    }

    public async Task<LoginResponse> LoginAsync(string email, string password)
    {
        var user = await _userRepository.GetByEmailAsync(email);

        if (user == null)
            throw new UnauthorizedAccessException("Email hoặc mật khẩu không đúng");

        if (user.DeletedAt != null || user.Status != "active")
            throw new UnauthorizedAccessException("Tài khoản không hoạt động");

        if (string.IsNullOrEmpty(user.PasswordHash) ||
            !BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
            throw new UnauthorizedAccessException("Email hoặc mật khẩu không đúng");

      
        var accessToken = _jwtService.GenerateAccessToken(user);
        var refreshToken = _jwtService.GenerateRefreshToken();

     
        await _jwtService.SaveRefreshTokenAsync(user.Id, refreshToken);

        user.LastLoginAt = DateTime.UtcNow;
        var requiresVibeQuiz = await ResolveRequiresVibeQuizAsync(user, CancellationToken.None);
        await _userRepository.UpdateAsync(user);

        return new LoginResponse
        {
            UserId = user.Id,
            Email = user.Email,
            Role = user.Role,
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            RequiresVibeQuiz = requiresVibeQuiz
        };
    }

    
    public async Task<LoginResponse> RefreshTokenAsync(string refreshToken)
    {
        var storedToken = await _jwtService.GetRefreshTokenAsync(refreshToken);

        if (storedToken == null)
            throw new UnauthorizedAccessException("Invalid hoặc expired refresh token");

        // Revoke old refresh token
        await _jwtService.RevokeRefreshTokenAsync(refreshToken);

        // Generate new tokens
        var user = storedToken.User;
        var newAccessToken = _jwtService.GenerateAccessToken(user);
        var newRefreshToken = _jwtService.GenerateRefreshToken();

        // Save new refresh token
        await _jwtService.SaveRefreshTokenAsync(user.Id, newRefreshToken);

        var beforeQuizFlag = user.VibeQuizCompletedAt;
        var requiresVibeQuiz = await ResolveRequiresVibeQuizAsync(user, CancellationToken.None);
        if (beforeQuizFlag != user.VibeQuizCompletedAt)
            await _userRepository.UpdateAsync(user);

        return new LoginResponse
        {
            UserId = user.Id,
            Email = user.Email,
            Role = user.Role,
            AccessToken = newAccessToken,
            RefreshToken = newRefreshToken,
            RequiresVibeQuiz = requiresVibeQuiz
        };
    }

    public async Task RegisterSetPasswordAsync(
       string email,
        string password,
        string confirmPassword
    )
    {
        // 1. Validate password
        if (password != confirmPassword)
            throw new Exception("Password không khớp");

        // 2. Check user đã tồn tại chưa
        var existingUser = await _userRepository.GetByEmailAsync(email);
        if (existingUser != null)
            throw new Exception("Email đã được đăng ký");

        // 4. Create user
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = email,
            Role = "traveler",
            Status = "active",
            CreatedAt = DateTime.UtcNow
        };

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(password);

        await _userRepository.CreateAsync(user);

        // 5. Mark OTP đã dùng
        await _emailOtpRepository.MarkAllUsedByEmailAsync(email);
    }


    public async Task LogoutAsync(string refreshToken)
    {
        await _jwtService.RevokeRefreshTokenAsync(refreshToken);
    }

    /// <summary>
    /// Traveler cần quiz chỉ khi chưa có travel style trên profile và chưa đánh dấu xong quiz.
    /// Đã tự chọn travel style trên profile ⇒ coi như đủ onboarding (gán VibeQuizCompletedAt; caller lưu DB khi cần).
    /// </summary>
    private async Task<bool> ResolveRequiresVibeQuizAsync(User user, CancellationToken cancellationToken)
    {
        if (!string.Equals(user.Role, AppRoles.Traveler, StringComparison.OrdinalIgnoreCase))
            return false;
        if (user.VibeQuizCompletedAt.HasValue)
            return false;

        var profile = await _userProfileRepository.GetByUserIdAsync(user.Id, cancellationToken);
        if (profile?.TravelStyle is { Count: > 0 })
        {
            user.VibeQuizCompletedAt = DateTime.UtcNow;
            user.UpdatedAt = DateTime.UtcNow;
            return false;
        }

        return true;
    }
}