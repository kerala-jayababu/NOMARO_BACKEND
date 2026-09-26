using System.ComponentModel.DataAnnotations;

namespace Nomaro.API.Models
{
    public class VacationMode
    {
        [Key]
        public int IdVacationMode { get; set; }
        public int IdEmployee { get; set; }
        public DateTime VacationFrom { get; set; }
        public DateTime VacationTo { get; set; }
        public int IdSubstitueEmployee { get; set; }
        public string? ReasonForVacation { get; set; }
    }
}

