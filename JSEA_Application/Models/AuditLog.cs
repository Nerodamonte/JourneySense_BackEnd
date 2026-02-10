using JSEA_Application.Enums;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Net;

namespace JSEA_Application.Models;

[Table("audit_logs")]
public partial class AuditLog
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; }

    [Column("user_id")]
    public Guid? UserId { get; set; }

    [Column("entity_type")]
    [StringLength(100)]
    public string? EntityType { get; set; }

    [Column("entity_id")]
    public Guid? EntityId { get; set; }

    [Column("old_values", TypeName = "jsonb")]
    public string? OldValues { get; set; }

    [Column("new_values", TypeName = "jsonb")]
    public string? NewValues { get; set; }

    [Column("ip_address")]
    public IPAddress? IpAddress { get; set; }

    [Column("user_agent")]
    public string? UserAgent { get; set; }

    [Column("created_at")]
    public DateTime? CreatedAt { get; set; }

    [ForeignKey("UserId")]
    [InverseProperty("AuditLogs")]
    public virtual User? User { get; set; }
    [Column("action_type")]
    public ActionType ActionType { get; set; }
}
