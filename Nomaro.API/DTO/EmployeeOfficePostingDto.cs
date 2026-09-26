using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Nomaro.API.DTO
{
    public class EmployeeOfficePostingDto
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

        [NotMapped]
        public string? EmployeeCode { get; set; }
        [NotMapped]
        public string? EmployeeName { get; set; }
        [NotMapped]
        public string? OfficeCode { get; set; }
        [NotMapped]
        public string? OfficeName { get; set; }
    }
}
