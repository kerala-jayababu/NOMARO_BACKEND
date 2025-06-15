using System.ComponentModel.DataAnnotations;

namespace Georgetown_Internationsl_Academy.API.Models.Time___Attendance.Shift
{
    public class ShiftEmployee
    {
        [Key]
        public int IdShiftEmployee { get; set; }
        public int IdShift { get; set; }
        public int IdEmployee { get; set; }
    }
}
