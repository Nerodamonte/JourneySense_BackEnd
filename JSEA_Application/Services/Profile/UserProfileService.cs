using JSEA_Application.Constants;
using JSEA_Application.DTOs.Request.Profile;
using JSEA_Application.DTOs.Respone.Profile;
using JSEA_Application.Enums;
using JSEA_Application.Interfaces;
using JSEA_Application.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace JSEA_Application.Services.Profile;

public class UserProfileService : IUserProfileService
{
    private readonly IUserProfileRepository _userProfileRepository;
    private readonly IUserRepository _userRepository;
    private readonly ITravelStyleTextGenerator _travelStyleTextGenerator;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<UserProfileService> _logger;

    public UserProfileService(
        IUserProfileRepository userProfileRepository,
        IUserRepository userRepository,
        ITravelStyleTextGenerator travelStyleTextGenerator,
        IServiceScopeFactory scopeFactory,
        ILogger<UserProfileService> logger)
    {
        _userProfileRepository = userProfileRepository;
        _userRepository = userRepository;
        _travelStyleTextGenerator = travelStyleTextGenerator;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public async Task UpdateProfileAsync(
        Guid userId,
        UpdateProfileRequest request,
        CancellationToken cancellationToken = default)
    {
        var userForRole = await _userRepository.GetByIdAsync(userId);
        if (userForRole == null)
            throw new UnauthorizedAccessException("User không tồn tại hoặc không hợp lệ.");

        var isPortalUser = IsPortalRole(userForRole.Role);

        if (request.Phone != null)
        {
            if (userForRole.Phone != request.Phone)
            {
                userForRole.Phone = request.Phone;
                userForRole.PhoneVerified = false;
                userForRole.UpdatedAt = DateTime.UtcNow;
                await _userRepository.UpdateAsync(userForRole);
            }
        }

        var profile = await _userProfileRepository.GetByUserIdAsync(userId, cancellationToken);
        var isNew = profile == null;

        var hasExistingTravelStyle = profile?.TravelStyle != null && profile.TravelStyle.Count > 0;
        var incomingTravelStyle = request.TravelStyle;

        // Traveler: lần đầu bắt buộc travel style cho suggest pipeline. Admin/staff: không dùng.
        if (!isPortalUser
            && !hasExistingTravelStyle
            && (incomingTravelStyle == null || incomingTravelStyle.Count == 0))
            throw new InvalidOperationException("Vui lòng chọn ít nhất 1 travel style.");

        profile ??= new UserProfile
        {
            Id = Guid.NewGuid(),
            UserId = userId
        };

        if (request.FullName != null)
            profile.FullName = request.FullName;

        if (request.AvatarUrl != null)
            profile.AvatarUrl = request.AvatarUrl;

        if (request.Bio != null)
            profile.Bio = request.Bio;

        if (request.AccessibilityNeeds != null)
            profile.AccessibilityNeeds = request.AccessibilityNeeds;

        List<VibeType>? refineVibesInBackground = null;

        // TravelStyle + TravelStyleText: chỉ traveler; admin/staff bỏ qua request.TravelStyle.
        if (!isPortalUser && incomingTravelStyle != null)
        {
            if (incomingTravelStyle.Count == 0)
                throw new InvalidOperationException("Vui lòng chọn ít nhất 1 travel style.");

            var incomingStrings = incomingTravelStyle
                .Select(v => v.ToString())
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .ToList();

            var existingStrings = profile.TravelStyle ?? new List<string>();

            var incomingSet = new HashSet<string>(incomingStrings, StringComparer.OrdinalIgnoreCase);
            var existingSet = new HashSet<string>(existingStrings, StringComparer.OrdinalIgnoreCase);

            var travelStyleChanged = !incomingSet.SetEquals(existingSet);

            if (travelStyleChanged)
            {
                profile.TravelStyle = incomingStrings;

                if (isNew)
                {
                    // Lần đầu: chờ Gemini để embedding suggest ổn định ngay sau đăng ký profile.
                    var generatedText = await _travelStyleTextGenerator.GenerateAsync(incomingTravelStyle, cancellationToken);
                    profile.TravelStyleText = !string.IsNullOrEmpty(generatedText)
                        ? generatedText
                        : BuildFallbackTravelStyleText(incomingStrings);
                }
                else
                {
                    // Đã có profile: trả API nhanh — text tạm cho embedding; Gemini chỉnh sau ở nền.
                    profile.TravelStyleText = BuildFallbackTravelStyleText(incomingStrings);
                    refineVibesInBackground = incomingTravelStyle.ToList();
                }
            }
        }

        if (isNew)
            await _userProfileRepository.CreateAsync(profile, cancellationToken);
        else
            await _userProfileRepository.UpdateAsync(profile, cancellationToken);

        // Skip quiz nhưng tự chọn travel style trên profile ⇒ coi như đã onboarding (giống submit quiz).
        if (!isPortalUser
            && string.Equals(userForRole.Role, AppRoles.Traveler, StringComparison.OrdinalIgnoreCase)
            && profile.TravelStyle is { Count: > 0 }
            && !userForRole.VibeQuizCompletedAt.HasValue)
        {
            userForRole.VibeQuizCompletedAt = DateTime.UtcNow;
            userForRole.UpdatedAt = DateTime.UtcNow;
            await _userRepository.UpdateAsync(userForRole);
        }

        if (refineVibesInBackground != null)
            StartTravelStyleTextRefinement(userId, refineVibesInBackground);
    }

    /// <summary>Mô tả ngắn sync từ danh sách vibe (đủ để embed tạm cho đến khi Gemini xong).</summary>
    private static string BuildFallbackTravelStyleText(IReadOnlyList<string> styleStrings) =>
        "Phong cach du lich: " + string.Join(", ", styleStrings);

    /// <summary>Làm mịn travel_style_text bằng Gemini; không chặn HTTP request.</summary>
    private void StartTravelStyleTextRefinement(Guid userId, IReadOnlyList<VibeType> vibes)
    {
        var vibesCopy = vibes.ToList();
        _ = RefineTravelStyleTextAsync(userId, vibesCopy);
    }

    private async Task RefineTravelStyleTextAsync(Guid userId, List<VibeType> vibesCopy)
    {
        try
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            var gen = scope.ServiceProvider.GetRequiredService<ITravelStyleTextGenerator>();
            var repo = scope.ServiceProvider.GetRequiredService<IUserProfileRepository>();

            var text = await gen.GenerateAsync(vibesCopy, CancellationToken.None);
            if (string.IsNullOrEmpty(text))
                return;

            var p = await repo.GetByUserIdAsync(userId, CancellationToken.None);
            if (p?.TravelStyle == null)
                return;

            var currentKey = string.Join("|", p.TravelStyle.OrderBy(x => x, StringComparer.OrdinalIgnoreCase));
            var expectedKey = string.Join("|", vibesCopy.Select(v => v.ToString()).OrderBy(x => x, StringComparer.OrdinalIgnoreCase));
            if (!string.Equals(currentKey, expectedKey, StringComparison.Ordinal))
                return;

            p.TravelStyleText = text;
            await repo.UpdateAsync(p, CancellationToken.None);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Refine travel_style_text (Gemini) failed for user {UserId}", userId);
        }
    }

    public async Task<ProfileResponse> GetProfileAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null)
            throw new UnauthorizedAccessException("User không tồn tại hoặc không hợp lệ.");

        var profile = await _userProfileRepository.GetByUserIdAsync(userId, cancellationToken);

        var isPortalUser = IsPortalRole(user.Role);

        List<VibeType>? travelStyle = null;
        int? points = null;

        if (!isPortalUser)
        {
            travelStyle = new List<VibeType>();
            if (profile?.TravelStyle != null)
            {
                foreach (var s in profile.TravelStyle)
                {
                    if (Enum.TryParse<VibeType>(s, ignoreCase: true, out var vibe))
                        travelStyle.Add(vibe);
                }
            }

            points = profile?.RewardPoints ?? 0;
        }

        bool? requiresVibeQuiz = null;
        if (string.Equals(user.Role, AppRoles.Traveler, StringComparison.OrdinalIgnoreCase))
        {
            var hasStyles = profile?.TravelStyle != null && profile.TravelStyle.Count > 0;
            requiresVibeQuiz = !user.VibeQuizCompletedAt.HasValue && !hasStyles;
        }

        return new ProfileResponse
        {
            UserId = userId,
            Role = user.Role,
            Email = user.Email,
            Phone = user.Phone,

            FullName = profile?.FullName,
            AvatarUrl = profile?.AvatarUrl,
            Bio = profile?.Bio,
            AccessibilityNeeds = profile?.AccessibilityNeeds,

            TravelStyle = travelStyle,
            Point = points,
            RequiresVibeQuiz = requiresVibeQuiz
        };
    }

    private static bool IsPortalRole(string role) =>
        string.Equals(role, AppRoles.Admin, StringComparison.OrdinalIgnoreCase)
        || string.Equals(role, AppRoles.Staff, StringComparison.OrdinalIgnoreCase);
}
