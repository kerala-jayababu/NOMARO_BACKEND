namespace Nomaro.API.DTO
{
    public class EmployeeWithoutSalaryApprovalDto
    {
        public int IdEmployee { get; set; }
        public string? EmployeeCode { get; set; }
        public string? EmployeeName { get; set; }
        public int IdDesignation { get; set; }
        public string? DesignationName { get; set; }
        public int IdDepartment { get; set; }
        public string? DepartmentName { get; set; }
        public DateTime? JoiningDate { get; set; }
        public string? Gender { get; set; }
        public string? EmailID { get; set; }
        public string? PhoneNumber1 { get; set; }
        public string? PhoneNumber2 { get; set; }
        public string? CurrentStatus { get; set; }
        public string? Status { get; set; }

        public string? OverTimeAllowedStatus { get; set; }
    }
}

