using System.ComponentModel.DataAnnotations;

namespace Georgetown_Internationsl_Academy.API.Models
{
    public class MaternityLeaveSalaryEntity
    {
        [Key]
        public int? IdMaternityLeaveSalary { get; set; }
        public int IdEmployee { get; set; }
        public DateTime MaternityLeaveFrom { get; set; }
        public DateTime MaternityLeaveTo { get; set; }     
        public decimal MaternityLeaveNetSalary { get; set; }    
        public int? IdSalaryMonthFrom { get; set; }
        public int? IdSalaryMonthTo { get; set; }      
        public decimal? TotalEarnings { get; set; }
        public decimal? TotalDeductions { get; set; }
  
    }
}
