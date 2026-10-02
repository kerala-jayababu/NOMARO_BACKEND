using System.ComponentModel.DataAnnotations;

namespace Nomaro.API.Models
{
    public class FinancialYears
    {
        [Key]
        public int IdFinancialYear { get; set; }
        public DateTime? FinancialYearFrom {  get; set; } 
        public DateTime? FinancialYearTo {  get; set; } 
        public string? FinancialYearName { get; set; }
    }
}
