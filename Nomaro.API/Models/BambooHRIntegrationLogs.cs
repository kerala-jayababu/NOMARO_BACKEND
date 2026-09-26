using System.ComponentModel.DataAnnotations;

namespace Nomaro.API.Models
{
    public class BambooHRIntegrationLogs
    {
        [Key]
        public int IdBambooHRIntegrationLog { get; set; }

        [Required]
        [StringLength(50)]
        public string? EntityType { get; set; }

        [Required]
        [StringLength(50)]
        public string? EntityActionType { get; set; }

        [Required]
        [StringLength(20)]
        public string? IntegrationStatus { get; set; }

        [Required]
        public DateTime IntegrationDate { get; set; }

        [Required]
        public string? IntegrationActionDetails { get; set; }
    }
}

