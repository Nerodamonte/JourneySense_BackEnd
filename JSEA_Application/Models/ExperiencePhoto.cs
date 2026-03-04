using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace JSEA_Application.Models;

[Table("experience_photos")]
public partial class ExperiencePhoto
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; }

    [Column("experience_id")]
    public Guid ExperienceId { get; set; }

    [Column("created_by_user_id")]
    public Guid? CreatedByUserId { get; set; }

    [Column("photo_url")]
    [StringLength(500)]
    public string PhotoUrl { get; set; } = null!;

    [Column("thumbnail_url")]
    [StringLength(500)]
    public string? ThumbnailUrl { get; set; }

    [Column("caption")]
    public string? Caption { get; set; }

    [Column("is_cover")]
    public bool? IsCover { get; set; }

    [Column("uploaded_at")]
    public DateTime? UploadedAt { get; set; }

    [ForeignKey("CreatedByUserId")]
    [InverseProperty("ExperiencePhotos")]
    public virtual User? CreatedByUser { get; set; }

    [ForeignKey("ExperienceId")]
    [InverseProperty("ExperiencePhotos")]
    public virtual Experience Experience { get; set; } = null!;
}
