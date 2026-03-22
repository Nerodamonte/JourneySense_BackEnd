using JSEA_Application.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JSEA_Application.DTOs.Respone.Profile
{
    public class ProfileResponse
    {
        public Guid UserId { get; set; }

        public string? Email { get; set; }
        public string? Phone { get; set; }

        public string? FullName { get; set; }
        public string? AvatarUrl { get; set; }
        public string? Bio { get; set; }
        public string? AccessibilityNeeds { get; set; }

        public List<VibeType> TravelStyle { get; set; } = new();

        public int RewardPoints { get; set; }
    }
}
