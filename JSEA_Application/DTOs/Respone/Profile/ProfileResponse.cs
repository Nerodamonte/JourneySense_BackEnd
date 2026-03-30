using JSEA_Application.Enums;
using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace JSEA_Application.DTOs.Respone.Profile
{
    public class ProfileResponse
    {
        public Guid UserId { get; set; }

        /// <summary>Khớp JWT/DB: admin | staff | traveler</summary>
        public string Role { get; set; } = null!;

        public string? Email { get; set; }
        public string? Phone { get; set; }

        public string? FullName { get; set; }
        public string? AvatarUrl { get; set; }
        public string? Bio { get; set; }
        public string? AccessibilityNeeds { get; set; }

        /// <summary>Chỉ traveler; admin/staff không trả field này (JSON bỏ qua khi null).</summary>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public List<VibeType>? TravelStyle { get; set; }

        /// <summary>Điểm thưởng — chỉ traveler; admin/staff không trả field này.</summary>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public int? Point { get; set; }

        /// <summary>Traveler: true nếu vẫn cần quiz (chưa có travel style); đã có style hoặc đã xong quiz thì false; admin/staff: null.</summary>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public bool? RequiresVibeQuiz { get; set; }
    }
}
