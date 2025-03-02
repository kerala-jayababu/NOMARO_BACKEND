using System.ComponentModel.DataAnnotations;

namespace Georgetown_Internationsl_Academy.API.Models
{
    public class ScheduledDeductionDetails
    {
        [Key]
        public int? IdScheduledSalaryDeductionDetail { get; set; }
        public int IdScheduledSalaryDeduction { get; set; }
        public int IdSalaryMonth { get; set; }
        public decimal AmountTobeDeducted { get; set; }
        public decimal? AmountDeducted { get; set; }
    }
}
