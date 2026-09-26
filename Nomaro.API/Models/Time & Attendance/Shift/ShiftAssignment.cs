using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Nomaro.API.Models.Time___Attendance.Shift
{
    public class ShiftAssignment
    {
        [Key]  
        public int? IdShiftAssignment { get; set; }
        public int IdEmployee { get; set; }
        public int IdShift { get; set; }
        public int IdShiftSchedule { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public int TotalDurationMinutes { get; set; }
        public decimal TotalDurationHours { get; set; }
        public bool? AttendanceStatus { get; set; }
    }
}

