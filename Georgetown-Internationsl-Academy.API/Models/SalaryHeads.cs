using System.ComponentModel.DataAnnotations;

namespace Georgetown_Internationsl_Academy.API.Models
{
    public class SalaryHeads
    {
        [Key]
        public int IdSalaryHead { get; set; }
        public string SalaryHeadCode { get; set; }
        public string SalaryHeadName { get; set; }
        public string HeadType { get; set; }
        public bool IsTaxable { get; set; }
        public bool IsActive { get; set; }
        public string CalculationMethod { get; set; }
        public int? IdPercentageSalaryHead { get; set; }
        public decimal? PercentageValue { get; set; }
        public decimal? FixedValue { get; set; }
        public string CustomFormula { get; set; }
        public int CreatedBy { get; set; }
        public DateTime CreatedOn { get; set; }
        public int? ModifiedBy { get; set; }
        public DateTime? ModifiedOn { get; set; }
        public bool IsLOPSalaryHead { get; set; }
    }
}
