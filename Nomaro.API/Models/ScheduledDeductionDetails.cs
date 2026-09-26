using System.ComponentModel.DataAnnotations;

namespace Nomaro.API.Models
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

