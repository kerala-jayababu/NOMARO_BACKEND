using System.ComponentModel.DataAnnotations;

namespace Georgetown_Internationsl_Academy.API.Models
{
    public class ScheduledSalaryDeduction
    {   
        [Key]
        public int IdScheduledSalaryDeduction { get; set; }
        public int? IdEmployee { get; set; }
        public decimal TotalAmount { get; set; }
        public int DeductionFromSalaryMonth { get; set; }
        public int DeductionToSalaryMonth { get; set; }
        public DateTime DeductionFromSalaryMonthDate { get; set; }
        public DateTime DeductionToSalaryMonthDate { get; set; }
        public int AllocatingSalaryHead { get; set; }
        public int MonthCount { get; set; }
        public decimal MonthlyDeductableAmount { get; set; }
    }
}
