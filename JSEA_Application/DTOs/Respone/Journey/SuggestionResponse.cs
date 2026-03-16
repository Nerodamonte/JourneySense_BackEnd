using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JSEA_Application.DTOs.Respone.Journey
{
    /// <summary>
    /// Một Experience được gợi ý từ pipeline v11 (embedding + scoring).
    /// Trả về khi GPS user vào gần segment.
    /// </summary>
    public class SuggestionResponse
    {
        public Guid SuggestionId { get; set; }
        public Guid ExperienceId { get; set; }

        /// <summary>Segment này thuộc route nào — FE dùng để pin marker đúng tuyến.</summary>
        public Guid SegmentId { get; set; }
        public string? Name { get; set; }
        public string? CategoryName { get; set; }
        public string? Address { get; set; }
        public string? City { get; set; }
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }

        /// <summary>Ảnh bìa của experience, null nếu chưa có.</summary>
        public string? CoverPhotoUrl { get; set; }

        /// <summary>Giá tham khảo. VD: Free | &lt;50k | 50-100k | &gt;100k</summary>
        public string? PriceRange { get; set; }

        /// <summary>Mức độ đông đúc. quiet | normal | busy</summary>
        public string? CrowdLevel { get; set; }

        /// <summary>Giờ mở cửa theo từng ngày trong tuần (jsonb). VD: {"mon":"8:00-21:00",...}</summary>
        public string? OpeningHours { get; set; }

        /// <summary>Phương tiện có thể tiếp cận. VD: ["walking","motorbike","car"]</summary>
        public List<string>? AccessibleBy { get; set; }

        /// <summary>Điểm đánh giá trung bình (1-5).</summary>
        public decimal? AvgRating { get; set; }

        /// <summary>Tổng số lượt đánh giá.</summary>
        public int? TotalRatings { get; set; }

        /// <summary>Khoảng cách lệch khỏi tuyến chính (mét).</summary>
        public int? DetourDistanceMeters { get; set; }

        /// <summary>Thời gian detour ước tính (phút), tính từ Goong khi suggest.</summary>
        public int? DetourTimeMinutes { get; set; }

        /// <summary>Cosine similarity giữa user vector và experience vector.</summary>
        public decimal? CosineScore { get; set; }

        /// <summary>Score khoảng cách = 1 / ST_Distance.</summary>
        public decimal? DistanceScore { get; set; }

        /// <summary>Score tổng hợp cuối = cosine x distance x boosts x quality_score.</summary>
        public decimal? FinalSimilarity { get; set; }

        /// <summary>
        /// Insight từ Gemini RAG. NULL cho đến khi user tap vào suggestion.
        /// Frontend gọi endpoint riêng để trigger generate.
        /// </summary>
        public string? AiInsight { get; set; }
    }

}