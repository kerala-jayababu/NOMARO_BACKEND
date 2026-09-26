using System.ComponentModel.DataAnnotations;

namespace Nomaro.API.Services.Implimentation.Time___Attendance.BambooHR
{
    public class BambooHRIntegrationLog
    {
        [Key]
        public int IdBambooHRIntegrationLog { get; set; }
        public string EntityType { get; set; } 
        public string EntityActionType { get; set; } 
        public string IntegrationStatus { get; set; } 
        public DateTime IntegrationDate { get; set; } 
        public string IntegrationActionDetails { get; set; }
    }
}

