using System.ComponentModel.DataAnnotations;
using JSEA_Application.Enums;

namespace JSEA_Application.DTOs.Request.Journey;

public class LogSuggestionInteractionRequest
{
    [Required]
    public InteractionType InteractionType { get; set; }
}
