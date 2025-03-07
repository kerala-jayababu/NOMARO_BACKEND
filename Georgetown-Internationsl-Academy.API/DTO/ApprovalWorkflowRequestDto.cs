using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Georgetown_Internationsl_Academy.API.DTO
{
    public class ApprovalWorkflowRequestDto
    {
        [Required(ErrorMessage = "EntityTablePrimaryKeyID is required.")]
        public int EntityTablePrimaryKeyID { get; set; }

        [Required(ErrorMessage = "EntityCode is required.")]
        public string EntityCode { get; set; }

        [Required(ErrorMessage = "Status is required.")]
        public string Status { get; set; }

        public string? RejectReason { get; set; }

        [NotMapped]
        public int? IdPayRollScreen { get; set; }
    }
}
