namespace Nomaro.API.DTO
{
    public class EmployeeShiftResult
    {
        public int IdEmployee { get; set; }
        public DateTime? StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public string IsOnLeave { get; set; } = "";
        public string IsWorkingDay { get; set; } = "";
        public string EmployeeName { get; set; } = "";
        public string EmailId { get; set; } = "";
        public string? StatusDetails { get; set; } = "";
    }
    public class NotClockedEmployeeWithShiftDto
    {
        public int IdEmployee { get; set; }
        public string EmployeeCode { get; set; } = "";
        public string EmployeeName { get; set; } = "";
        public string EmailId { get; set; } = "";
        public DateTime? ExpectedStartTime { get; set; }
        public DateTime? ExpectedEndTime { get; set; }
        public string StatusDetails { get; set; } = "";
        public string DepartmentName { get; set; } = "";
        public string DesignationName { get; set; } = "";
    }
}

