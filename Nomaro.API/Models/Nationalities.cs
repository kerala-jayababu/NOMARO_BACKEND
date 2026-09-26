using System.ComponentModel.DataAnnotations;

namespace Nomaro.API.Models
{
    public class Nationalities
    {
        [Key]
        [StringLength(50)]
        public string Nationality { get; set; } = null!;
    }
}

