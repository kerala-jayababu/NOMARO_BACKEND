using System.ComponentModel.DataAnnotations;

namespace Nomaro.API.Models.Time___Attendance.Shift
{
    public class ShiftSchedule
    {
        [Key]
        public int IdShiftSchedule { get; set; }
        public int IdShift { get; set; }
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
        public int TotalDurationMinutes { get; set; }
        public decimal TotalDurationHours { get; set; }
        public string WorkDays { get; set; } = string.Empty;
    }
}

