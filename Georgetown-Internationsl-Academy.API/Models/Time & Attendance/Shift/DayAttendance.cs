using System.ComponentModel.DataAnnotations;

namespace Georgetown_Internationsl_Academy.API.Models.Time___Attendance.Shift
{
    public class DayAttendance
    {
        [Key]
        public int IdDayAttendance { get; set; }
        public int IdEmployee { get; set; }
        public DateTime AttendanceDate { get; set; }
        public string RegularDayType { get; set; } = string.Empty;
        public int? IdShiftSchedule { get; set; }
        public bool IsWorkingDayForEmployee { get; set; }
        public bool IsLeaveApplied { get; set; }
        public int? IdEmployeeLeave { get; set; }
        public DateTime? FirstInDateTime { get; set; }
        public DateTime? LastOutDateTime { get; set; }
        public DateTime? ExpectedInDateTime { get; set; }
        public DateTime? ExpectedOutDateTime { get; set; }
        public int? TotalDurationINMinutes { get; set; }
        public decimal? TotalDurationINHours { get; set; }
        public int? ActualDurationINMinutes { get; set; }
        public decimal? ActualDurationINHours { get; set; }
        public int? ExpectedDurationInMinutes { get; set; }
        public decimal? ExpectedDurationInHours { get; set; }
        public int? MinuteDifference { get; set; }
        public decimal? HourDifference { get; set; }
        public int? AllowedTolerenceInMinutes { get; set; }
        public decimal? DeficitHours { get; set; }
        public string? DeficitHoursReason { get; set; }
        public bool IsAttendanceOK { get; set; }
        public string? TotalDurationHoursText { get; set; }
        public string? ActualHoursText { get; set; }
        public string? StatusDetails { get; set; }
        public string? ReasonForShortTime { get; set; }
        public string? TimeSheetApprovalStatus { get; set; }
        public int? IdApprovedBy { get; set; }
        public DateTime? ApprovedDateTime { get; set; }
    }

}
