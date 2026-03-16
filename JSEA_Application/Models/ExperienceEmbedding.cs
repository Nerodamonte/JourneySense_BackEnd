using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Pgvector;
namespace JSEA_Application.Models;

[Table("experience_embeddings")]
public class ExperienceEmbedding
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; }

    [Column("experience_id")]
    public Guid ExperienceId { get; set; }

    [Column("metadata_string", TypeName = "text")]
    public string MetadataString { get; set; } = null!;

   
    [Column("embedding", TypeName = "vector(3072)")]
    public Vector Embedding { get; set; } = null!;

    [Column("embedded_at")]
    public DateTime EmbeddedAt { get; set; }

    [Column("updated_at")]
    public DateTime? UpdatedAt { get; set; }

    [ForeignKey("ExperienceId")]
    [InverseProperty("ExperienceEmbedding")]
    public virtual Experience Experience { get; set; } = null!;
}
