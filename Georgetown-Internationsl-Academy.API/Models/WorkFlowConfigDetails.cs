using System.ComponentModel.DataAnnotations;

namespace Georgetown_Internationsl_Academy.API.Models
{
    public class WorkFlowConfigDetails
    {
        [Key]
        public int IdWorkFlowConfigDetail { get; set; }
        public int IdWorkFlowConfig { get; set; }
        public int LevelNumber { get; set; }
        public string ApprovalAuthorityType { get; set; }
        public int? ApprovalAuthorityID { get; set; }
    }
}
