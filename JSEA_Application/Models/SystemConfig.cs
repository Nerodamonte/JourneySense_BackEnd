using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace JSEA_Application.Models;

[Table("system_configs")]
[Index("ConfigKey", Name = "system_configs_config_key_key", IsUnique = true)]
public partial class SystemConfig
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; }

    [Column("config_key")]
    [StringLength(100)]
    public string? ConfigKey { get; set; }

    [Column("config_value", TypeName = "jsonb")]
    public string? ConfigValue { get; set; }

    [Column("description")]
    public string? Description { get; set; }

    [Column("updated_by_user_id")]
    public Guid? UpdatedByUserId { get; set; }

    [Column("updated_at")]
    public DateTime? UpdatedAt { get; set; }

    [ForeignKey("UpdatedByUserId")]
    [InverseProperty("SystemConfigs")]
    public virtual User? UpdatedByUser { get; set; }
}
