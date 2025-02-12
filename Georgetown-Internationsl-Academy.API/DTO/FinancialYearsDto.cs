using System.ComponentModel.DataAnnotations;

namespace Georgetown_Internationsl_Academy.API.DTO
{
    public class FinancialYearsDto
    {
        [Key]
        public int IdFinancialYear { get; set; }
        public DateTime? FinancialYearFrom { get; set; }
        public DateTime? FinancialYearTo { get; set; }
    }
}
