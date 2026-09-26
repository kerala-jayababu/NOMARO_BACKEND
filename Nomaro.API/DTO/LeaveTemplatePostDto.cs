using System.ComponentModel.DataAnnotations;

namespace Nomaro.API.DTO
{
    public class LeaveTemplatePostDto
    {
        [Key]
        public int IdLeaveTemplate { get; set; }

        /// <summary>
        /// Policy / Template name
        /// </summary>
        public string? LeaveTemplateName { get; set; }

        /// <summary>
        /// Template description
        /// </summary>
        public string? LeaveTemplateDesc { get; set; }

        /// <summary>
        /// Applicable year
        /// </summary>
        public int IdYear { get; set; }

        /// <summary>
        /// Audit fields
        /// </summary>
        public int CreatedBy { get; set; }
        public DateTime CreatedAt { get; set; }

        public int? UpdatedBy { get; set; }
        public DateTime? UpdatedAt { get; set; }

        /// <summary>
        /// SUBMITTED / APPROVED / REJECTED
        /// </summary>
        public string ApprovlStatus { get; set; }

        /// <summary>
        /// User who approved the template
        /// </summary>
        public int? IdApprovedBy { get; set; }
    }
}

