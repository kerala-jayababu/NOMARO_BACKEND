using System.ComponentModel.DataAnnotations;

namespace Nomaro.API.DTO
{
    public class WorkYearsDto
    {
        [Key]
        public int IdWorkYear { get; set; }
        public DateTime? WorkDateFrom { get; set; }
        public DateTime? WorkDateTo { get; set; }
        public string? DisplayText { get; set; }

    }
}

