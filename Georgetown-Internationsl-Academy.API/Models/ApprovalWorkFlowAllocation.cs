using System.ComponentModel.DataAnnotations;

namespace Georgetown_Internationsl_Academy.API.Models
{
    public class ApprovalWorkFlowAllocation
    {
        [Key]
        public int IdApprovalWorkFlow { get; set; }
        public int IdWorkFlowConfig { get; set; }
        public string EntityCode { get; set; }
        public int EntityTablePrimaryKeyID { get; set; }
        public int CycleIndex { get; set; }
        public int LevelNumber { get; set; }
        public int SourceIdEmployee { get; set; }
        public string TargetIdEmployee { get; set; } 
        public string? ActionStatus { get; set; }
        public DateTime? ActionDate { get; set; }
        public int? ActionedBy { get; set; }
        public string? RejectionRemarks { get; set; }
        public DateTime SentDate { get; set; }
    }
}
