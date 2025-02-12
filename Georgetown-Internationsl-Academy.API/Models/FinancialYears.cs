using System.ComponentModel.DataAnnotations;

namespace Georgetown_Internationsl_Academy.API.Models
{
    public class FinancialYears
    {
        [Key]
        public int IdFinancialYear { get; set; }
        public DateTime? FinancialYearFrom {  get; set; } 
        public DateTime? FinancialYearTo {  get; set; } 

    }
}
