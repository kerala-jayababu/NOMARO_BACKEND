namespace Nomaro.API.DTO
{
    public class ExitClearanceForDepartmentUserDto
    {

        public int IdExitCase { get; set; }
        public int IdEmployee { get; set; }
        public string EmployeeCode { get; set; }
        public int IdExitType { get; set; }
        public int IdExitReason { get; set; }
        public int LogginedEmployeeIdDepartment { get; set; }
        public DateTime? InitiationDate { get; set; }
        public string? EmployeeReasonDetails { get; set; }
        public DateTime? ProposedLWD { get; set; }
        public DateTime? ApprovedLWD { get; set; }
        public string? EmployeeName { get; set; }
        public string? CurrentStatus { get; set; }
    }
}

