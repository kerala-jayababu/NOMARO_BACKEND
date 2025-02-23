namespace Georgetown_Internationsl_Academy.API.DTO
{
    public class SalaryGenerationDetailsDto
    {
        public int IdEmployeeSalary { get; set; }
        public int IdEmployee { get; set; }
        public decimal TotalEarnings { get; set; }
        public decimal TotalDeductions { get; set; }
        public decimal TaxAmountAccounted { get; set; }
        public string ApprovalStatus { get; set; } = string.Empty;

        // Employee Information
        public string EmployeeCode { get; set; } = string.Empty;
        public string EmployeeName { get; set; } = string.Empty; 
        public int IdDesignation { get; set; }
        public string DesignationName { get; set; } = string.Empty;
        public int IdDepartment { get; set; }
        public string DepartmentName { get; set; } = string.Empty;
        public DateTime JoiningDate { get; set; }
        public DateTime? LastWorkingDay { get; set; }
        public string Gender { get; set; } = string.Empty;
        public string EmailID { get; set; } = string.Empty;
        public string? PhoneNumber1 { get; set; } = string.Empty;
        public string? PhoneNumber2 { get; set; } = string.Empty;
        // Salary Details (List of Salary Detail Records)
        public List<SalaryDetailDto> SalaryDetails { get; set; } = new List<SalaryDetailDto>();
    }

    public class SalaryDetailDto
    {
        public int IdEmployeeSalaryDetail { get; set; }
        public int IdSalaryHead { get; set; }
        public string SalaryHeadName { get; set; } = string.Empty;
        public string SalaryHeadType { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public decimal AmountInUSD { get; set; }
    }
}
