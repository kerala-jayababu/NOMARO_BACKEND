using System.ComponentModel.DataAnnotations;

namespace Georgetown_Internationsl_Academy.API.DTO
{
    public class NationalitiesDto
    {
        [Key]
        public string Nationality { get; set; } = null!;
    }
}
