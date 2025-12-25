using System.ComponentModel.DataAnnotations;

namespace Georgetown_Internationsl_Academy.API.Models
{
    public class Countries
    {
        [Key]
        public int IdCountry { get; set; }

        [Required]
        [MaxLength(3)]
        public string CountryCode { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string CountryName { get; set; } = string.Empty;
    }
}
