using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace JSEA_Application.Models;

[Table("notifications")]
public partial class Notification
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; }

    [Column("user_id")]
    public Guid UserId { get; set; }

    [Column("notification_type")]
    [StringLength(100)]
    public string NotificationType { get; set; } = null!;

    [Column("title")]
    [StringLength(255)]
    public string Title { get; set; } = null!;

    [Column("message")]
    public string? Message { get; set; }

    [Column("related_experience_id")]
    public Guid? RelatedExperienceId { get; set; }

    [Column("related_journey_id")]
    public Guid? RelatedJourneyId { get; set; }

    [Column("is_read")]
    public bool? IsRead { get; set; }

    [Column("read_at")]
    public DateTime? ReadAt { get; set; }

    [Column("created_at")]
    public DateTime? CreatedAt { get; set; }

    [ForeignKey("UserId")]
    [InverseProperty("Notifications")]
    public virtual User User { get; set; } = null!;
}
