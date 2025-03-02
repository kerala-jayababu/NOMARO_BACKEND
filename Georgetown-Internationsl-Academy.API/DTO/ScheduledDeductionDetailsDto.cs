using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Georgetown_Internationsl_Academy.API.DTO
{
    public class ScheduledDeductionDetailsDto
    {
        [Key]
        public int? IdScheduledSalaryDeductionDetail { get; set; }
        public int? IdScheduledSalaryDeduction { get; set; }
        public int IdSalaryMonth { get; set; }
        public decimal AmountTobeDeducted { get; set; }
        public decimal? AmountDeducted { get; set; }

        [NotMapped]
        public string? SalaryMonthText { get; set; }
    }
}
