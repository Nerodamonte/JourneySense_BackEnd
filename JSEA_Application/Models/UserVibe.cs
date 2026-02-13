using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JSEA_Application.Models;
[Table("user_vibes")]
public partial class UserVibe
{
    [Column("user_profile_id")]
    public Guid UserProfileId { get; set; }

    [Column("travel_style_id")]
    public Guid TravelStyleId { get; set; }

    [Column("selected_at")]
    public DateTime SelectedAt { get; set; } = DateTime.UtcNow;

    [ForeignKey("UserProfileId")]
    public virtual UserProfile UserProfile { get; set; } = null!;

    [ForeignKey("TravelStyleId")]
    public virtual TravelStyle TravelStyle { get; set; } = null!;
}
