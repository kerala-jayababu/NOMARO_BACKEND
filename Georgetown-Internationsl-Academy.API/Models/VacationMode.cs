using System.ComponentModel.DataAnnotations;

namespace Georgetown_Internationsl_Academy.API.Models
{
    public class VacationMode
    {
        [Key]
        public int IdVacationMode { get; set; }
        public int IdEmployee { get; set; }
        public DateTime VacationFrom { get; set; }
        public DateTime VacationTo { get; set; }
        public int IdSubstitueEmployee { get; set; }
    }
}
