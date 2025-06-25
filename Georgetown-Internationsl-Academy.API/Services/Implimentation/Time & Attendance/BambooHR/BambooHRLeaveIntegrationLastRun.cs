using System.ComponentModel.DataAnnotations;

namespace Georgetown_Internationsl_Academy.API.Services.Implimentation.Time___Attendance.BambooHR
{
    public class BambooHRLeaveIntegrationLastRun
    {
        [Key]
        public DateTime LeaveIntegrationLastRunDate { get; set; }
    }
}
