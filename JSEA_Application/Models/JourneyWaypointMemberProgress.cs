using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace JSEA_Application.Models;

[Table("journey_waypoint_member_progress")]
public class JourneyWaypointMemberProgress
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; }

    [Column("journey_member_id")]
    public Guid JourneyMemberId { get; set; }

    [Column("journey_waypoint_id")]
    public Guid? JourneyWaypointId { get; set; }

    [Column("milestone_kind")]
    [StringLength(20)]
    public string MilestoneKind { get; set; } = null!;

    [Column("arrived_at")]
    public DateTime? ArrivedAt { get; set; }

    [Column("departed_at")]
    public DateTime? DepartedAt { get; set; }

    [Column("skipped")]
    public bool Skipped { get; set; }

    [ForeignKey(nameof(JourneyMemberId))]
    [InverseProperty(nameof(JourneyMember.WaypointProgress))]
    public virtual JourneyMember JourneyMember { get; set; } = null!;

    [ForeignKey(nameof(JourneyWaypointId))]
    public virtual JourneyWaypoint? JourneyWaypoint { get; set; }
}
