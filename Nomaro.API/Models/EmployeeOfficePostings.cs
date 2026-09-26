using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Nomaro.API.Models
{
    public class EmployeeOfficePostings
    {
        [Key]
        public int IdEmployeeOfficePosting { get; set; }
        public int IdEmployee { get; set; }
        public int IdOffice { get; set; }
        public DateTime PostingFromDate { get; set; }
        public DateTime? PostingToDate { get; set; }
        public string? PostingType { get; set; }
        public string? TransferOrderNumber { get; set; }
        public bool IsCurrentPosting { get; set; }
        public string? PostingRemarks { get; set; }
        public string? ApprovalStatus { get; set; }
        public int? IdApprovedBy { get; set; }
        public DateTime? ApprovedDateTime { get; set; }
        public int? IdCreatedBy { get; set; }
        public DateTime CreatedDateTime { get; set; }
        public int? IdModifiedBy { get; set; }
        public DateTime? ModifiedDateTime { get; set; }

        // Navigation Property
        [ForeignKey("IdOffice")]
        public virtual Offices Office { get; set; }
    }
}
