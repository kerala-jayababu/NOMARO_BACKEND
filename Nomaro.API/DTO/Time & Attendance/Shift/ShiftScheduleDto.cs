using System.ComponentModel.DataAnnotations.Schema;

namespace Nomaro.API.DTO.Time___Attendance.Shift
{
    public class ShiftScheduleDto
    {
        public int? IdShiftSchedule { get; set; }
        public int IdShift { get; set; }
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
        public int? TotalDurationMinutes { get; set; }
        public decimal? TotalDurationHours { get; set; }
        public string WorkDays { get; set; } = string.Empty;
        [NotMapped]
        public string? ShiftName { get; set; } = string.Empty;
    }
}


