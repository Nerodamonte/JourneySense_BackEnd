using JSEA_Application.Interfaces;
using JSEA_Application.Enums;
using JSEA_Application.Interfaces.Auth;
using JSEA_Application.DTOs.Respone.Auth;
using Microsoft.Extensions.Configuration;

namespace JSEA_Application.Services.Auth;

public class AuthService : IAuthService
{
    private readonly IUserRepository _userRepository;
    private readonly IJwtService _jwtService;
    private readonly IConfiguration _configuration;

    public AuthService(
        IUserRepository userRepository,
        IJwtService jwtService,
        IConfiguration configuration)
    {
        _userRepository = userRepository;
        _jwtService = jwtService;
        _configuration = configuration;
    }

    public async Task<LoginResponse> LoginAsync(string email, string password)
    {
        var user = _userRepository.GetByEmail(email);

        if (user == null)
            throw new UnauthorizedAccessException("Email hoặc mật khẩu không đúng");

        if (user.DeletedAt != null || user.Status != UserStatus.Active)
            throw new UnauthorizedAccessException("Tài khoản không hoạt động");

        if (string.IsNullOrEmpty(user.PasswordHash) ||
            !BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
            throw new UnauthorizedAccessException("Email hoặc mật khẩu không đúng");

      
        var accessToken = _jwtService.GenerateAccessToken(user);
        var refreshToken = _jwtService.GenerateRefreshToken();

     
        await _jwtService.SaveRefreshTokenAsync(user.Id, refreshToken);

        // Update last login
        user.LastLoginAt = DateTime.UtcNow;
        _userRepository.Update(user);

        return new LoginResponse
        {
            UserId = user.Id,
            Email = user.Email,
            Role = user.Role,
            AccessToken = accessToken,
            RefreshToken = refreshToken 
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

        return new LoginResponse
        {
            UserId = user.Id,
            Email = user.Email,
            Role = user.Role,
            AccessToken = newAccessToken,
            RefreshToken = newRefreshToken
        };
    }

  
    public async Task LogoutAsync(string refreshToken)
    {
        await _jwtService.RevokeRefreshTokenAsync(refreshToken);
    }
}