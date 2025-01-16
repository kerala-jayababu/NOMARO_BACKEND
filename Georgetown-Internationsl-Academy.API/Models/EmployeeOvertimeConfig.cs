using System.ComponentModel.DataAnnotations;

namespace Georgetown_Internationsl_Academy.API.Models
{
    public class EmployeeOvertimeConfig
    {
        [Key]
        public int IdEmployeeOvertimeConfig { get; set; }
        public int IdEmployee { get; set; }
        public string DayType { get; set; }
        public decimal StandardRate { get; set; }
        public decimal? DayRate { get; set; }
    }
}
