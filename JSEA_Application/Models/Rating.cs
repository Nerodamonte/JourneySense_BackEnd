using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace JSEA_Application.Models;

[Table("ratings")]
public partial class Rating
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; }

    [Column("visit_id")]
    public Guid VisitId { get; set; }

    [Column("rating")]
    public int Rating1 { get; set; }

    [Column("created_at")]
    public DateTime? CreatedAt { get; set; }

    [ForeignKey("VisitId")]
    [InverseProperty("Rating")]
    public virtual Visit Visit { get; set; } = null!;
}
