using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace JSEA_Application.Models;

[Table("event_occurrences")]
public partial class EventOccurrence
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; }

    [Column("event_id")]
    public Guid? EventId { get; set; }

    [Column("occurrence_start")]
    public DateTime? OccurrenceStart { get; set; }

    [Column("occurrence_end")]
    public DateTime? OccurrenceEnd { get; set; }

    [ForeignKey("EventId")]
    [InverseProperty("EventOccurrences")]
    public virtual Event? Event { get; set; }
}
