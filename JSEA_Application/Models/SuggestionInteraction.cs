using JSEA_Application.Enums;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace JSEA_Application.Models;

[Table("suggestion_interactions")]
public partial class SuggestionInteraction
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; }

    [Column("suggestion_id")]
    public Guid? SuggestionId { get; set; }

    [Column("interaction_type")]
    [StringLength(50)]
    public InteractionType InteractionType { get; set; }

    [Column("interacted_at")]
    public DateTime? InteractedAt { get; set; }

    [ForeignKey("SuggestionId")]
    [InverseProperty("SuggestionInteractions")]
    public virtual JourneySuggestion? Suggestion { get; set; }
}
