using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JSEA_Application.Models;

[Table("travel_styles")]
public partial class TravelStyle
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; }

    [Column("name")]
    [StringLength(100)]
    public string Name { get; set; } = null!;

    [Column("descripton")]
    [StringLength(100)]
    public string? Descripton { get; set; }

   
    public virtual ICollection<UserVibe> UserVibes { get; set; } = new List<UserVibe>();
}