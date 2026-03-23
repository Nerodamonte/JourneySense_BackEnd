using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace JSEA_Application.Models;

[Table("shared_journeys")]
public partial class SharedJourney
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; }

    [Column("journey_id")]
    public Guid JourneyId { get; set; }

    [Column("user_id")]
    public Guid UserId { get; set; }

    [Column("share_code")]
    [StringLength(20)]
    public string ShareCode { get; set; } = null!;

    [Column("view_count")]
    public int ViewCount { get; set; }

    [Column("is_active")]
    public bool IsActive { get; set; } = true;

    [Column("created_at")]
    public DateTime? CreatedAt { get; set; }

    [ForeignKey("JourneyId")]
    [InverseProperty("SharedJourneys")]
    public virtual Journey Journey { get; set; } = null!;

    [ForeignKey("UserId")]
    [InverseProperty("SharedJourneys")]
    public virtual User User { get; set; } = null!;
}
