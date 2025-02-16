public class LatestEmployeeSalaryConfigDto
{
    public int IdEmployeeSalaryConfig { get; set; }
    public int IdEmployee { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string? MiddleName { get; set; }
    public string LastName { get; set; } = string.Empty;
    public DateTime ValidFrom { get; set; }
    public DateTime? ValidTo { get; set; }
    public int? IdSalaryTemplate { get; set; }
    public int CreatedBy { get; set; }
    public DateTime CreatedOn { get; set; }
    public string ApprovalStatus { get; set; } = string.Empty;
    public bool ActiveStatus { get; set; }
    public decimal TotalEarnings { get; set; }
    public decimal TotalDeductions { get; set; }
    public decimal NetSalary { get; set; }

    // Salary Breakdown
    public List<SalaryComponentDto>? SalaryComponents { get; set; } = new();
}

public class SalaryComponentDto
{
    public int IdSalaryHead { get; set; }
    public string SalaryHeadCode { get; set; } = string.Empty;
    public string SalaryHeadName { get; set; } = string.Empty;
    public string HeadType { get; set; } = string.Empty; // Earning or Deduction
    public bool IsTaxable { get; set; }
    public bool IsActive { get; set; }
    public string CalculationMethod { get; set; } = string.Empty;
    public int? IdPercentageSalaryHead { get; set; }
    public decimal? PercentageValue { get; set; }
    public decimal? FixedValue { get; set; }
    public string? CustomFormula { get; set; }
    public int? OrderNumber { get; set; }
}
