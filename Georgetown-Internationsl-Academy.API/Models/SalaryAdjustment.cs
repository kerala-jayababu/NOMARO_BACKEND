using System.ComponentModel.DataAnnotations;

namespace Georgetown_Internationsl_Academy.API.Models
{
    public class SalaryAdjustment
    {
        [Key]
        public int IdSalaryAdjustment { get; set; }
        public int IdEmployee { get; set; }
        public DateTime PayAdjustmentDate { get; set; }
        public string PayAdjustmentDetails { get; set; }
        public int AllocatingSalaryHead { get; set; }
        public DateTime? AllocationSalaryMonthDate { get; set; }
        public char EarningOrDeduction { get; set; }
        public int AllocatingSalaryMonth { get; set; }
        public decimal Amount { get; set; }
        public string? Remarks { get; set; }
        public Boolean? IsTaxable { get; set; }
    }
}
