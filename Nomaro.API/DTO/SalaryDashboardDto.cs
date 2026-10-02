namespace Nomaro.API.DTO
{
    /// <summary>Filter values for the Salary Dashboard screen.</summary>
    public class SalaryDashboardFilterDto
    {
        public List<SalaryDashboardMonthOptionDto> Months { get; set; } = new();
        public List<SalaryDashboardOptionDto> Offices { get; set; } = new();
        public List<SalaryDashboardOptionDto> Departments { get; set; } = new();
    }

    public class SalaryDashboardMonthOptionDto
    {
        public int IdSalaryMonth { get; set; }
        public string SalaryMonthText { get; set; } = string.Empty;
        public DateTime SalaryMonthDate { get; set; }
    }

    public class SalaryDashboardOptionDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    /// <summary>Everything the Salary Dashboard needs for one salary month and filter.</summary>
    public class SalaryDashboardDto
    {
        public int IdSalaryMonth { get; set; }
        public string SalaryMonthText { get; set; } = string.Empty;
        public string? PreviousMonthText { get; set; }
        public bool ApprovedOnly { get; set; }

        public SalaryDashboardSummaryDto Summary { get; set; } = new();
        public SalaryDashboardSummaryDto? PreviousSummary { get; set; }

        /// <summary>Selected month and up to 11 months before it, oldest first.</summary>
        public List<SalaryDashboardMonthDto> Trend { get; set; } = new();

        /// <summary>Salary records of the selected month by approval status (ignores the Approved-only filter).</summary>
        public List<SalaryDashboardCountDto> StatusCounts { get; set; } = new();

        public List<SalaryDashboardGroupDto> ByDepartment { get; set; } = new();
        public List<SalaryDashboardGroupDto> ByOffice { get; set; } = new();
        public List<SalaryDashboardCellDto> DepartmentOffice { get; set; } = new();

        public List<SalaryDashboardStatutoryDto> Statutory { get; set; } = new();

        public List<SalaryDashboardBridgeStepDto> GrossBridge { get; set; } = new();
        public List<SalaryDashboardHeadDto> HeadChanges { get; set; } = new();
        public List<SalaryDashboardEmployeeChangeDto> EmployeeChanges { get; set; } = new();

        public List<SalaryDashboardBandDto> SalaryBands { get; set; } = new();
        public List<SalaryDashboardRangeDto> DesignationRanges { get; set; } = new();
    }

    public class SalaryDashboardSummaryDto
    {
        public int EmployeesPaid { get; set; }
        public decimal GrossEarnings { get; set; }
        public decimal TotalDeductions { get; set; }
        public decimal NetPay { get; set; }
        public decimal EmployerContribution { get; set; }
        public decimal EmployerCost { get; set; }
        public decimal AverageCostPerEmployee { get; set; }
        public decimal StatutoryTotal { get; set; }
        public decimal TdsDeducted { get; set; }
        public int JoinedPayroll { get; set; }
        public int LeftPayroll { get; set; }
    }

    public class SalaryDashboardMonthDto
    {
        public int IdSalaryMonth { get; set; }
        public string SalaryMonthText { get; set; } = string.Empty;
        public int EmployeesPaid { get; set; }
        public decimal GrossEarnings { get; set; }
        public decimal TotalDeductions { get; set; }
        public decimal NetPay { get; set; }
        public decimal EmployerContribution { get; set; }
        public decimal EmployerCost { get; set; }
        public decimal PF { get; set; }
        public decimal ESI { get; set; }
        public decimal PT { get; set; }
        public decimal LWF { get; set; }
        public decimal TDS { get; set; }
    }

    public class SalaryDashboardGroupDto
    {
        public int? Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int Employees { get; set; }
        public decimal GrossEarnings { get; set; }
        public decimal NetPay { get; set; }
        public decimal EmployerCost { get; set; }
        public int PreviousEmployees { get; set; }
        public decimal PreviousEmployerCost { get; set; }
        /// <summary>Employer cost per month for the trend months (same order as Trend).</summary>
        public List<decimal> EmployerCostTrend { get; set; } = new();
    }

    public class SalaryDashboardCellDto
    {
        public int? IdDepartment { get; set; }
        public string DepartmentName { get; set; } = string.Empty;
        public int? IdOffice { get; set; }
        public string OfficeName { get; set; } = string.Empty;
        public int Employees { get; set; }
        public decimal EmployerCost { get; set; }
    }

    public class SalaryDashboardStatutoryDto
    {
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public decimal EmployeeShare { get; set; }
        public decimal EmployerShare { get; set; }
        public decimal Total { get; set; }
        public decimal PreviousTotal { get; set; }
        public int Employees { get; set; }
        /// <summary>Usual due date of the payment (statutory practice); null when it depends on the state.</summary>
        public DateTime? DueDate { get; set; }
    }

    public class SalaryDashboardBridgeStepDto
    {
        public string Label { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public bool IsTotal { get; set; }
    }

    public class SalaryDashboardHeadDto
    {
        public int? IdSalaryHead { get; set; }
        public string SalaryHeadName { get; set; } = string.Empty;
        public string HeadType { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public decimal PreviousAmount { get; set; }
        public decimal Change { get; set; }
    }

    public class SalaryDashboardEmployeeChangeDto
    {
        public int IdEmployee { get; set; }
        public string? EmployeeCode { get; set; }
        public string EmployeeName { get; set; } = string.Empty;
        public string? DepartmentName { get; set; }
        public string? DesignationName { get; set; }
        public string? OfficeName { get; set; }
        public decimal PreviousNetPay { get; set; }
        public decimal NetPay { get; set; }
        public decimal ChangePercent { get; set; }
        public string? Reasons { get; set; }
    }

    public class SalaryDashboardBandDto
    {
        public string Label { get; set; } = string.Empty;
        public int Employees { get; set; }
        public decimal GrossEarnings { get; set; }
    }

    public class SalaryDashboardRangeDto
    {
        public string Name { get; set; } = string.Empty;
        public int Employees { get; set; }
        public decimal Minimum { get; set; }
        public decimal Average { get; set; }
        public decimal Maximum { get; set; }
    }

    public class SalaryDashboardCountDto
    {
        public string Name { get; set; } = string.Empty;
        public int Count { get; set; }
    }
}
