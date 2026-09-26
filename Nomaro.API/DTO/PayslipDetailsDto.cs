using System.ComponentModel.DataAnnotations.Schema;

namespace Nomaro.API.DTO
{
    public class PayslipDetailsDto
    {
        public int IdEmployeeSalary {  get; set; }
        public decimal TotalEarnings {  get; set; }
        public decimal TotalDeductions { get; set; }
        public int  IdEmployee { get; set; }

        public DateTime? ApprovedDate { get; set; }

        public List<SelfPortalEmployeeSalaryDetailsDto>? EmployeeSalaryDetails { get; set; }

        [NotMapped]
        public string? EmployeeCode { get; set; }
        [NotMapped]
        public string? EmployeeName { get; set; }
        [NotMapped]
        public int? IdDesignation { get; set; }
        [NotMapped]
        public string? DesignationName { get; set; }
        [NotMapped]
        public int? IdDepartment { get; set; }
        [NotMapped]
        public string? DepartmentName { get; set; }
        [NotMapped]
        public DateTime? JoiningDate { get; set; }
        [NotMapped]
        public string? Gender { get; set; }
        [NotMapped]
        public string? EmailID { get; set; }
        [NotMapped]
        public string? PhoneNumber1 { get; set; }
        [NotMapped]
        public string? PhoneNumber2 { get; set; }
        [NotMapped]
        public string? CurrentStatus { get; set; }

        [NotMapped]
        public string? OverTimeAllowedStatus { get; set; }
    }
}

