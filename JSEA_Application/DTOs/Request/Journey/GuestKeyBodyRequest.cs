using System.ComponentModel.DataAnnotations;

namespace JSEA_Application.DTOs.Request.Journey;

public class GuestKeyBodyRequest
{
    [Required]
    public Guid GuestKey { get; set; }
}
