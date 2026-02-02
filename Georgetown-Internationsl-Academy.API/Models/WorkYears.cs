using System.ComponentModel.DataAnnotations;

namespace Georgetown_Internationsl_Academy.API.Models
{
    public class WorkYears
    {
        [Key]
        public int IdWorkYear { get; set; }
        public DateTime? WorkDateFrom {  get; set; } 
        public DateTime? WorkDateTo {  get; set; }
        public string? DisplayText { get; set; }

    }
}
