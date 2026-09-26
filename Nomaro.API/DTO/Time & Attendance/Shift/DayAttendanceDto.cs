namespace Nomaro.API.DTO.Time___Attendance.Shift
{
    public class DayAttendanceDto
    {
        public int IdEmployee { get; set; }
        public string EmployeeName { get; set; }
        public string EmployeeCode { get; set; }
        public int IdDesignation { get; set; }
        public string DesignationName { get; set; }
        public int IdDepartment { get; set; }
        public string DepartmentName { get; set; }
        public DateTime AttendanceDate { get; set; }
        public string RegularDayType { get; set; }
        public int? IdShiftSchedule { get; set; }
        public DateTime? FirstInDateTime { get; set; }
        public DateTime? LastOutDateTime { get; set; }
        public DateTime? ExpectedInDateTime { get; set; }
        public DateTime? ExpectedOutDateTime { get; set; }
        public int? TotalDurationInMinutes { get; set; }
        public decimal? TotalDurationInHours { get; set; }
        public int? ActualDurationInMinutes { get; set; }
        public decimal? ActualDurationInHours { get; set; }
        public int? ExpectedDurationInMinutes { get; set; }
        public int? MinuteDifference { get; set; }
        public int? AllowedTolerenceInMinutes { get; set; }
        public decimal? DeficitHours { get; set; }
        public string? TotalDurationHoursText { get; set; }
        public string ActualHoursText { get; set; }
        public string? StatusType { get; set; }
        public string? StatusDetails { get; set; }
        public string? ReasonForShortTime { get; set; }
        public string? TimeSheetApprovalStatus { get; set; }
        public int IdDayAttendance { get; set; }
    }

}

