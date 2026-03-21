using JSEA_Application.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JSEA_Application.DTOs.Request.Profile
{
    public class UpdateProfileRequest
    {
        [StringLength(255)]
        public string? FullName { get; set; }

         [StringLength(20)]
        public string? Phone { get; set; }

        [StringLength(500)]
        public string? AvatarUrl { get; set; }

        public string? Bio { get; set; }

        public string? AccessibilityNeeds { get; set; }

        /// <summary>
        /// TravelStyle optional khi update bình thường.
        /// Nếu user chưa có TravelStyle trong DB (lần đầu) thì backend sẽ yêu cầu truyền ít nhất 1.
        /// </summary>
        public List<VibeType>? TravelStyle { get; set; }
    }
}
