namespace Nomaro.API.DTO
{
    /// <summary>Request for the Salary Template / Employee Salary Config grid calculation.</summary>
    public class SalaryStructureCalculationRequestDto
    {
        public List<SalaryStructureRowDto> Rows { get; set; } = new();
    }

    /// <summary>
    /// One grid row. Input: IdSalaryHead plus FixedAmount (Fixed Amount heads) or PercentageValue (Percentage heads).
    /// Everything else is read-only information taken from SalaryHeads, and CalculatedValue is the monthly amount.
    /// </summary>
    public class SalaryStructureRowDto
    {
        public int IdSalaryHead { get; set; }
        public decimal? FixedAmount { get; set; }
        public decimal? PercentageValue { get; set; }

        public decimal CalculatedValue { get; set; }
        public string? SalaryHeadCode { get; set; }
        public string? SalaryHeadName { get; set; }
        public string? HeadType { get; set; }
        public string? CalculationMethod { get; set; }
        public int? IdPercentageSalaryHead { get; set; }
        public string? PercentageOfSalaryHeadName { get; set; }
        public string? CustomFormula { get; set; }
        public int? CalcSequence { get; set; }
        public string? PayFrequency { get; set; }
        public string? DisbursingMonths { get; set; }
        public bool IsIncludedInMonthlyTotals { get; set; }
        public string? PaidInNote { get; set; }
    }

    public class SalaryStructureResultDto
    {
        public List<SalaryStructureRowDto> Rows { get; set; } = new();
        public decimal TotalEarnings { get; set; }
        public decimal TotalDeductions { get; set; }
        public decimal TotalEmployerContribution { get; set; }
        public decimal NetSalary { get; set; }
        public decimal GrossMonthly { get; set; }
        public decimal CTCMonthly { get; set; }
        public decimal CTCAnnual { get; set; }
        public List<string> Errors { get; set; } = new();
    }

    /// <summary>Header information shown after an employee is selected on the Employee Salary Config screen.</summary>
    public class EmployeeSalaryStructureInfoDto
    {
        public int IdEmployee { get; set; }
        public int? IdCurrentEmployeeSalaryConfig { get; set; }
        public DateTime? CurrentValidFrom { get; set; }
        public decimal? CurrentNetSalary { get; set; }
        public bool HasPendingStructure { get; set; }
        public DateTime DefaultValidFrom { get; set; }
        public string DefaultRevisionReason { get; set; } = "JOINING";
        public List<string> Warnings { get; set; } = new();
    }
}
