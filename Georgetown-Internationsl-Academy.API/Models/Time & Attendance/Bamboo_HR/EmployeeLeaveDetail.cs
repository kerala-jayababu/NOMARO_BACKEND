using System.ComponentModel.DataAnnotations;

namespace Georgetown_Internationsl_Academy.API.Models.Time___Attendance.Bamboo_HR
{
    public class EmployeeLeaveDetail
    {
        [Key]
        public int IdEmployeeLeaveDetail { get; set; }
        public int IdEmployeeLeave { get; set; }
        public DateTime LeaveDate { get; set; }
        public bool AmountFlag { get; set; } 
        public decimal Amount { get; set; }
    }
}
