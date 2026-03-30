using JSEA_Application.DTOs.Request.Quiz;
using JSEA_Application.DTOs.Respone.Quiz;
using JSEA_Application.DTOs.Request.Profile;
using JSEA_Application.Enums;
using JSEA_Application.Interfaces;

namespace JSEA_Application.Services.Quiz;

public class VibeQuizService : IVibeQuizService
{
    private const string QuizId = "vibe-v1";
    private readonly IUserProfileService _userProfileService;
    private readonly IUserRepository _userRepository;

    public VibeQuizService(IUserProfileService userProfileService, IUserRepository userRepository)
    {
        _userProfileService = userProfileService;
        _userRepository = userRepository;
    }

    public VibeQuizResponse GetQuiz()
    {
        return new VibeQuizResponse
        {
            QuizId = QuizId,
            Questions = new List<VibeQuizQuestionResponse>
            {
                new()
                {
                    Id = "q1_plan_style",
                    Text = "Bạn thích lịch trình du lịch theo kiểu nào?",
                    MultiSelect = false,
                    MinSelect = 1,
                    MaxSelect = 1,
                    Options = new List<VibeQuizOptionResponse>
                    {
                        new() { Id = "o1", Text = "Đi thong thả, ít lịch, ưu tiên tận hưởng", Weights = W((VibeType.Relax,3),(VibeType.Chill,2)) },
                        new() { Id = "o2", Text = "Có kế hoạch vừa đủ, cân bằng trải nghiệm", Weights = W((VibeType.Explorer,2),(VibeType.LocalVibes,1)) },
                        new() { Id = "o3", Text = "Đi nhiều điểm, thích khám phá liên tục", Weights = W((VibeType.Explorer,3),(VibeType.Adventure,1)) },
                    }
                },
                new()
                {
                    Id = "q2_pick_place",
                    Text = "Khi chọn địa điểm, bạn ưu tiên điều gì nhất?",
                    MultiSelect = false,
                    MinSelect = 1,
                    MaxSelect = 1,
                    Options = new List<VibeQuizOptionResponse>
                    {
                        new() { Id = "o1", Text = "Đồ ăn ngon, quán xá hay", Weights = W((VibeType.Foodie,4)) },
                        new() { Id = "o2", Text = "Góc chụp đẹp, hình phải xịn", Weights = W((VibeType.Photographer,4)) },
                        new() { Id = "o3", Text = "Hoạt động mới lạ, có thử thách", Weights = W((VibeType.Adventure,4)) },
                        new() { Id = "o4", Text = "Không khí địa phương, văn hoá bản địa", Weights = W((VibeType.LocalVibes,4)) },
                        new() { Id = "o5", Text = "Yên tĩnh, dễ chịu, ít đông", Weights = W((VibeType.Relax,3),(VibeType.Chill,2)) },
                    }
                },
                new()
                {
                    Id = "q3_day_pace",
                    Text = "Một ngày du lịch lý tưởng của bạn là...",
                    MultiSelect = false,
                    MinSelect = 1,
                    MaxSelect = 1,
                    Options = new List<VibeQuizOptionResponse>
                    {
                        new() { Id = "o1", Text = "Sáng cà phê, trưa ăn ngon, chiều chill", Weights = W((VibeType.Chill,3),(VibeType.Foodie,1),(VibeType.Relax,1)) },
                        new() { Id = "o2", Text = "Đi bộ khám phá, ghé nhiều điểm thú vị", Weights = W((VibeType.Explorer,3),(VibeType.LocalVibes,1)) },
                        new() { Id = "o3", Text = "Trải nghiệm hoạt động (trek, chơi thể thao, thử cái mới)", Weights = W((VibeType.Adventure,3),(VibeType.Explorer,1)) },
                        new() { Id = "o4", Text = "Canh 'giờ vàng' để chụp ảnh đẹp", Weights = W((VibeType.Photographer,3),(VibeType.Chill,1)) },
                    }
                },
                new()
                {
                    Id = "q4_travel_memory",
                    Text = "Sau chuyến đi, bạn muốn nhớ điều gì nhất?",
                    MultiSelect = false,
                    MinSelect = 1,
                    MaxSelect = 1,
                    Options = new List<VibeQuizOptionResponse>
                    {
                        new() { Id = "o1", Text = "Cảm giác thư giãn, nạp lại năng lượng", Weights = W((VibeType.Relax,4)) },
                        new() { Id = "o2", Text = "Những món ngon và quán tủ", Weights = W((VibeType.Foodie,4)) },
                        new() { Id = "o3", Text = "Ảnh đẹp và khoảnh khắc đáng nhớ", Weights = W((VibeType.Photographer,4)) },
                        new() { Id = "o4", Text = "Câu chuyện địa phương / trải nghiệm văn hoá", Weights = W((VibeType.LocalVibes,4)) },
                        new() { Id = "o5", Text = "Những nơi mới khám phá lần đầu", Weights = W((VibeType.Explorer,4)) },
                        new() { Id = "o6", Text = "Vượt giới hạn bản thân", Weights = W((VibeType.Adventure,4)) },
                    }
                },
                new()
                {
                    Id = "q5_multi_select",
                    Text = "Chọn tối đa 2 điều bạn thường làm nhất khi du lịch",
                    MultiSelect = true,
                    MinSelect = 1,
                    MaxSelect = 2,
                    Options = new List<VibeQuizOptionResponse>
                    {
                        new() { Id = "o1", Text = "Thử món mới / food tour", Weights = W((VibeType.Foodie,2)) },
                        new() { Id = "o2", Text = "Săn ảnh / check-in", Weights = W((VibeType.Photographer,2)) },
                        new() { Id = "o3", Text = "Đi những chỗ ít người biết", Weights = W((VibeType.Explorer,2)) },
                        new() { Id = "o4", Text = "Hỏi chuyện người địa phương", Weights = W((VibeType.LocalVibes,2)) },
                        new() { Id = "o5", Text = "Chơi hoạt động mạo hiểm", Weights = W((VibeType.Adventure,2)) },
                        new() { Id = "o6", Text = "Nghỉ dưỡng / spa / cà phê yên tĩnh", Weights = W((VibeType.Relax,2),(VibeType.Chill,1)) },
                    }
                }
            }
        };
    }

    public async Task<SubmitVibeQuizResponse> SubmitAsync(Guid userId, SubmitVibeQuizRequest request, CancellationToken cancellationToken = default)
    {
        request ??= new SubmitVibeQuizRequest();
        if (!string.Equals(request.QuizId, QuizId, StringComparison.Ordinal))
            throw new InvalidOperationException("QuizId không hợp lệ.");

        var quiz = GetQuiz();
        var scores = Enum.GetValues<VibeType>().ToDictionary(v => v, _ => 0);

        foreach (var q in quiz.Questions)
        {
            request.Answers.TryGetValue(q.Id, out var selected);
            selected ??= new List<string>();

            if (selected.Count < q.MinSelect)
                continue;

            if (!q.MultiSelect && selected.Count > 1)
                selected = selected.Take(1).ToList();

            if (q.MultiSelect && selected.Count > q.MaxSelect)
                selected = selected.Take(q.MaxSelect).ToList();

            foreach (var optionId in selected)
            {
                var opt = q.Options.FirstOrDefault(o => o.Id == optionId);
                if (opt == null) continue;

                foreach (var kv in opt.Weights)
                    scores[kv.Key] += kv.Value;
            }
        }

        var top = request.Top;
        if (top < 1) top = 3;
        if (top > 3) top = 3;

        var vibes = scores
            .OrderByDescending(kv => kv.Value)
            .ThenBy(kv => kv.Key)
            .Where(kv => kv.Value > 0)
            .Take(top)
            .Select(kv => kv.Key)
            .ToList();

        if (vibes.Count == 0)
        {
            // Fallback nếu user skip hết: set mặc định nhẹ nhàng.
            vibes = new List<VibeType> { VibeType.Chill, VibeType.Relax };
        }

        if (request.ApplyToProfile)
        {
            await _userProfileService.UpdateProfileAsync(
                userId,
                new UpdateProfileRequest { TravelStyle = vibes },
                cancellationToken);
        }

        var user = await _userRepository.GetByIdAsync(userId);
        if (user != null)
        {
            user.VibeQuizCompletedAt = DateTime.UtcNow;
            user.UpdatedAt = DateTime.UtcNow;
            await _userRepository.UpdateAsync(user);
        }

        return new SubmitVibeQuizResponse
        {
            QuizId = QuizId,
            Vibes = vibes,
            Scores = scores
        };
    }

    private static Dictionary<VibeType, int> W(params (VibeType vibe, int weight)[] weights)
    {
        var dict = new Dictionary<VibeType, int>();
        foreach (var (vibe, weight) in weights)
        {
            if (weight <= 0) continue;
            dict[vibe] = dict.TryGetValue(vibe, out var cur) ? cur + weight : weight;
        }
        return dict;
    }
}
