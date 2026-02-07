using System.ComponentModel.DataAnnotations;

namespace Georgetown_Internationsl_Academy.API.Models
{
    public class Nationalities
    {
        [Key]
        [StringLength(50)]
        public string Nationality { get; set; } = null!;
    }
}
