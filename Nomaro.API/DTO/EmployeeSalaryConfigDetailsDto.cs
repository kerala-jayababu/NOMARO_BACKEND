using System.ComponentModel.DataAnnotations.Schema;

namespace Nomaro.API.DTO
{
    public class EmployeeSalaryConfigDetailsDto
    {
        public int? IdEmployeeSalaryConfigDetail { get; set; }
        public int IdEmployeeSalaryConfig { get; set; }
        public int IdSalaryHead { get; set; }
        public string CalculationMethod { get; set; }
        public decimal? FixedAmount { get; set; }
        public int? PercentageOfIdSalaryHead { get; set; }
        public decimal? PercentageValue { get; set; }
        public string? CustomFormula { get; set; }
        public decimal? SalaryAmount { get; set; }
        [NotMapped]
        public string? SalaryHeadName { get; set; }
        [NotMapped]
        public string? HeadType { get; set; }
        [NotMapped]
        public string? SalaryHeadCode { get; set; }

        [NotMapped]
        public string? PercentageOfIdSalaryHeadValue { get; set; }
    }
}

