namespace Georgetown_Internationsl_Academy.API.DTO.Time___Attendance.Shift
{
    public class ShiftAssignmentDto
    {
        public int? IdShiftAssignment { get; set; }
        public int IdEmployee { get; set; }
        public int IdShift { get; set; }
        public int IdShiftSchedule { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public int TotalDurationMinutes { get; set; }
        public decimal TotalDurationHours { get; set; }
        public bool? AttendanceStatus { get; set; }

        // [NotMapped] extras
        public string EmployeeCode { get; set; } = string.Empty;
        public string EmployeeName { get; set; } = string.Empty;
        public string ShiftName { get; set; } = string.Empty;
        public string Department { get; set; } = string.Empty;
        public string Designation { get; set; } = string.Empty;
        public int IdDepartment { get; set; }
        public int IdDesignation { get; set; }
    }

    public class CopyShiftAssignmentsRequest
    {
        public int IdShif { get; set; }
        public DateTime SourceDate { get; set; }
        public DateTime TargetDate { get; set; }
    }

}
