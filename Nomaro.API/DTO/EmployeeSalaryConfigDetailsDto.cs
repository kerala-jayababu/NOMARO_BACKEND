using System.ComponentModel.DataAnnotations.Schema;

namespace Nomaro.API.DTO
{
    public class EmployeeSalaryConfigDetailsDto
    {
        public int? IdEmployeeSalaryConfigDetail { get; set; }
        public int IdEmployeeSalaryConfig { get; set; }
        public int IdSalaryHead { get; set; }
        // Input: amount for Fixed Amount heads, percentage for Percentage heads
        public decimal? FixedAmount { get; set; }
        public decimal? PercentageValue { get; set; }
        // Calculated by the API
        public decimal? SalaryAmount { get; set; }

        // Read-only, taken from SalaryHeads (no longer stored in EmployeeSalaryConfigDetails)
        [NotMapped]
        public string? CalculationMethod { get; set; }
        [NotMapped]
        public int? PercentageOfIdSalaryHead { get; set; }
        [NotMapped]
        public string? PercentageOfIdSalaryHeadValue { get; set; }
        [NotMapped]
        public string? CustomFormula { get; set; }
        [NotMapped]
        public string? SalaryHeadName { get; set; }
        [NotMapped]
        public string? HeadType { get; set; }
        [NotMapped]
        public string? SalaryHeadCode { get; set; }
        [NotMapped]
        public int? CalcSequence { get; set; }
        [NotMapped]
        public string? PaidInNote { get; set; }
    }
}
