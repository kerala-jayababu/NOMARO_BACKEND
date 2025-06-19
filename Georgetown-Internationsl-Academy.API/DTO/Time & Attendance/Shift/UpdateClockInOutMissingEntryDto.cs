namespace Georgetown_Internationsl_Academy.API.DTO.Time___Attendance.Shift
{
    public class UpdateClockInOutMissingEntryDto
    {
        public int IdClockDetail { get; set; }
        public int IdEmployee { get; set; }
        public string ClockType { get; set; } = string.Empty;
        public DateTime Time { get; set; }
        public string? Reason { get; set; }
    }
}
