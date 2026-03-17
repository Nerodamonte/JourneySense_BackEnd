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

        [StringLength(500)]
        public string? AvatarUrl { get; set; }

        public string? Bio { get; set; }

        public string? AccessibilityNeeds { get; set; }

        /// <summary>Buộc chọn ít nhất 1 để generate travel_style_text cho suggest pipeline.</summary>
        [Required(ErrorMessage = "Vui lòng chọn ít nhất 1 travel style")]
        [MinLength(1, ErrorMessage = "Vui lòng chọn ít nhất 1 travel style")]
        public List<VibeType> TravelStyle { get; set; } = new();
    }
}
