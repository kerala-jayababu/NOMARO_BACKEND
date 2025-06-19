namespace Georgetown_Internationsl_Academy.API.DTO.Time___Attendance.Shift
{
    public class ApproveTimesheetDto
    {
        public int IdDayAttendance { get; set; }
        public int IdEmployee { get; set; }
        public string ApprovalStatus { get; set; } = string.Empty; 
        public string? RejectReasons { get; set; }
    }
}
