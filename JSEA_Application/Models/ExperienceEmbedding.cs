using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

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

    /// <summary>
    /// Embedding vector (768-d). Tạm thời không map bằng EF để tránh lỗi,
    /// nếu cần dùng có thể thao tác qua raw SQL hoặc cấu hình type mapping sau.
    /// </summary>
    [NotMapped]
    public float[] Embedding { get; set; } = Array.Empty<float>();

    [Column("embedded_at")]
    public DateTime EmbeddedAt { get; set; }

    [Column("updated_at")]
    public DateTime? UpdatedAt { get; set; }

    [ForeignKey("ExperienceId")]
    [InverseProperty("ExperienceEmbedding")]
    public virtual Experience Experience { get; set; } = null!;
}
