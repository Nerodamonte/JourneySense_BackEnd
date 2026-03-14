using JSEA_Application.DTOs.Respone.Journey;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JSEA_Application.Interfaces
{
    public interface ISuggestService
    {
        /// <summary>
        /// Pipeline gợi ý chính (v11). Gọi khi GPS user vào gần segment.
        ///
        /// Flow:
        ///   1. Hard filter (7 điều kiện)
        ///   2. Build User Metadata String → Gemini embed → User Vector
        ///   3. Cosine search trong candidates
        ///   4. Tính distance_score, boosts, quality_score
        ///   5. final_similarity = cosine × distance × boosts × quality
        ///   6. INSERT journey_suggestions (ai_insight = NULL)
        ///   7. Trả về list SuggestionResponse sắp xếp theo final_similarity DESC
        /// </summary>
        /// <param name="journeyId">Journey đang chạy.</param>
        /// <param name="segmentId">Segment GPS vừa trigger.</param>
        /// <param name="cancellationToken"></param>
        Task<List<SuggestionResponse>> GetSuggestionsAsync(
            Guid journeyId,
            Guid segmentId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Tạo AI insight cho một suggestion (RAG). Gọi khi user tap vào suggestion.
        ///
        /// Flow:
        ///   1. Lấy suggestion + experience + top 3 feedbacks + realtime context
        ///   2. Build prompt → Gemini generate
        ///   3. UPDATE journey_suggestions.ai_insight
        ///   4. Trả về insight string
        ///
        /// Nếu ai_insight đã có (đã generate trước) → trả về luôn, không gọi Gemini lại.
        /// </summary>
        /// <param name="suggestionId">Id của journey_suggestion cần generate insight.</param>
        /// <param name="cancellationToken"></param>
        Task<string?> GetAiInsightAsync(
            Guid suggestionId,
            CancellationToken cancellationToken = default);
    }

}
