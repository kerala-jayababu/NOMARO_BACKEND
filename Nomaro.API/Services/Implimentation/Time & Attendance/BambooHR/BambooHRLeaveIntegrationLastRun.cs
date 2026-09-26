using System.ComponentModel.DataAnnotations;

namespace Nomaro.API.Services.Implimentation.Time___Attendance.BambooHR
{
    public class BambooHRLeaveIntegrationLastRun
    {
        [Key]
        public int Id { get; set; }
        public DateTime LeaveIntegrationLastRunDate { get; set; }
    }

}

