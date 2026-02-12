using System;
using System.Collections.Generic;
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
    public Guid? ExperienceId { get; set; }

    [Column("photo_url")]
    [StringLength(500)]
    public string? PhotoUrl { get; set; }

    [Column("thumbnail_url")]
    [StringLength(500)]
    public string? ThumbnailUrl { get; set; }

    [Column("caption")]
    public string? Caption { get; set; }

    [Column("display_order")]
    public int? DisplayOrder { get; set; }

    [Column("uploaded_by_user_id")]
    public Guid? UploadedByUserId { get; set; }

    [Column("is_cover")]
    public bool? IsCover { get; set; }

    [ForeignKey("ExperienceId")]
    [InverseProperty("ExperiencePhotos")]
    public virtual MicroExperience? Experience { get; set; }
}
