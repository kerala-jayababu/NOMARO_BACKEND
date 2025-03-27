namespace Georgetown_Internationsl_Academy.API.DTO
{
    public class SalarySlipDto
    {
        public int IdEmployeeSalary { get; set; }
        public string IdSalaryMonth { get; set; }
        public string SalaryMonthText { get; set; }
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
        public string? EmailStatus { get; set; } = string.Empty;

    }
}
