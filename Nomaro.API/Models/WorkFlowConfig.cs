using System.ComponentModel.DataAnnotations;

namespace Nomaro.API.Models
{
    public class WorkFlowConfig
    {
        [Key]
        public int IdWorkFlowConfig { get; set; }
        public string EntityCode { get; set; }
        public string EntityName { get; set; }
        public int ApprovalCycleCount { get; set; }
        public string? MainTableName { get; set; }
        public string? MainColumnName { get; set; }
    }
}

