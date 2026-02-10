using System.ComponentModel.DataAnnotations.Schema;

namespace Georgetown_Internationsl_Academy.API.DTO
{
    public class SalaryDetailsEmployeeDto
    {
        [NotMapped]
        public int IdEmployee { get; set; }
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

        public List<EmployeeSalariesDto>? EmployeeSalaries { get; set; }


    }
}
