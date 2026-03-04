using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace JSEA_Application.Models;

[Table("categories")]
[Index("Name", Name = "categories_name_key", IsUnique = true)]
[Index("Slug", Name = "categories_slug_key", IsUnique = true)]
public partial class Category
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; }

    [Column("name")]
    [StringLength(100)]
    public string? Name { get; set; }

    [Column("slug")]
    [StringLength(100)]
    public string? Slug { get; set; }

    [Column("description")]
    public string? Description { get; set; }

    [Column("icon_url")]
    [StringLength(500)]
    public string? IconUrl { get; set; }

    [Column("display_order")]
    public int? DisplayOrder { get; set; }

    [Column("is_active")]
    public bool? IsActive { get; set; }

    [InverseProperty("Category")]
    public virtual ICollection<Experience> Experiences { get; set; } = new List<Experience>();
}
