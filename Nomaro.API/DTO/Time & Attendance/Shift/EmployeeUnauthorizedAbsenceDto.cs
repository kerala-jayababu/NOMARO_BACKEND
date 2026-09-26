namespace Nomaro.API.DTO.Time___Attendance.Shift
{
    public class EmployeeUnauthorizedAbsenceDto
    {
        public int IdEmployee { get; set; }
        public string EmployeeName { get; set; }
        public int IdDesignation { get; set; }
        public string DesignationName { get; set; }
        public int IdDepartment { get; set; }
        public string DepartmentName { get; set; }
        public DateTime AbsentDate { get; set; }
        public string? Reason { get; set; }
        public string? FilePath { get; set; }
        public bool LeaveAdjusted { get; set; }        
        public int? IdEmployeeLeave { get; set; }      
        public int? IdEmployeeLeaveDetails { get; set; }
    }
}

