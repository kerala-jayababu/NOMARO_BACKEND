namespace Georgetown_Internationsl_Academy.API.DTO.Time___Attendance.Shift
{
    public class UpdateShortTimeReasonDto
    {
        public int IdDayAttendance { get; set; }
        public int IdEmployee { get; set; }
        public string ReasonForShortTime { get; set; } = string.Empty;
    }
}
