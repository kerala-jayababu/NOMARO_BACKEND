namespace Nomaro.API.DTO
{
    public class OfficeShiftScheduleDto
    {
        public string ShiftName { get; set; } = string.Empty;
        public bool IsRegularShiftJustTimeChange { get; set; } = true;
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
        public string WorkDays { get; set; } = string.Empty;
    }
}