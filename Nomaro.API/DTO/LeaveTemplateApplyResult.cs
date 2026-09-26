using Org.BouncyCastle.Bcpg.OpenPgp;

namespace Nomaro.API.DTO
{
    public class LeaveTemplateApplyResult
    {
        public int IdEmployee { get; set; }
        public bool IsSuccess { get; set; }
        public string? Message { get; set; }
    }

    public class ApplyLeaveTemplateMultileEmployeesDto
    {
        public List<int> IdEmployees { get; set; }
        public int IdLeaveTemplate { get; set; }
        public int IdYear { get; set; }
    }

    public class EmpLeaveConfigDetailsDto
    {

        public int IdEmployee { get; set; }
        public string EmployeeCode { get; set; }
        public string EmployeeName { get; set; }
        public DateTime? JoiningDate { get; set; }
        public string DesignationName {get;set;}
        public string DepartmentName { get; set; }
        public string? Email { get; set; }
        public bool IsConfigured { get; set; }

    }
}

