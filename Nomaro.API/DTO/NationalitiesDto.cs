using System.ComponentModel.DataAnnotations;

namespace Nomaro.API.DTO
{
    public class NationalitiesDto
    {
        [Key]
        public string Nationality { get; set; } = null!;
    }
}

