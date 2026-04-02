using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace JSEA_Application.Models;

[Table("journey_members")]
public class JourneyMember
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; }

    [Column("journey_id")]
    public Guid JourneyId { get; set; }

    [Column("traveler_id")]
    public Guid? TravelerId { get; set; }

    [Column("guest_key")]
    public Guid? GuestKey { get; set; }

    [Column("display_name")]
    [StringLength(120)]
    public string DisplayName { get; set; } = null!;

    [Column("is_registered_user")]
    public bool IsRegisteredUser { get; set; } = true;

    [Column("role")]
    [StringLength(20)]
    public string Role { get; set; } = null!;

    [Column("is_active")]
    public bool IsActive { get; set; } = true;

    [Column("joined_at")]
    public DateTime JoinedAt { get; set; }

    [Column("left_at")]
    public DateTime? LeftAt { get; set; }

    [ForeignKey(nameof(JourneyId))]
    [InverseProperty(nameof(Journey.JourneyMembers))]
    public virtual Journey Journey { get; set; } = null!;

    [ForeignKey(nameof(TravelerId))]
    public virtual User? Traveler { get; set; }

    [InverseProperty(nameof(JourneyWaypointMemberProgress.JourneyMember))]
    public virtual ICollection<JourneyWaypointMemberProgress> WaypointProgress { get; set; } =
        new List<JourneyWaypointMemberProgress>();
}
