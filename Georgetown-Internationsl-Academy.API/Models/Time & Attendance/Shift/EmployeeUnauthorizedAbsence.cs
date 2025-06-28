using System.ComponentModel.DataAnnotations;

namespace Georgetown_Internationsl_Academy.API.Models.Time___Attendance.Shift
{
    public class EmployeeUnauthorizedAbsence
    {
        [Key]
        public int IdUnauthorizedAbsence { get; set; }     
        public int IdEmployee { get; set; }                
        public DateTime AbsentDate { get; set; }           
        public bool LeaveAdjusted { get; set; }            
        public int? IdEmployeeLeave { get; set; }          
        public int? IdEmployeeLeaveDetails { get; set; }   
        public string? Reason { get; set; }                 
        public string? FilePath { get; set; }
    }
}
