using System.ComponentModel.DataAnnotations;

namespace Nomaro.API.Models
{
    // What an employee may request through self-service (configuration).
    public class ProfileChangeRequestTypes
    {
        [Key]
        public int IdProfileChangeRequestType { get; set; }
        public string RequestCategory { get; set; }
        public string RequestTypeCode { get; set; }
        public string RequestTypeName { get; set; }
        public string EntityCode { get; set; }
        public bool ProofRequired { get; set; }
        public string? ProofHint { get; set; }
        public int DisplayOrder { get; set; }
        public bool IsActive { get; set; }
        public int? CreatedBy { get; set; }
        public DateTime CreatedDateTime { get; set; }
        public int? ModifiedBy { get; set; }
        public DateTime? ModifiedDateTime { get; set; }
    }

    // One row per employee request (header).
    public class ProfileChangeRequests
    {
        [Key]
        public int IdProfileChangeRequest { get; set; }
        public string? RequestNumber { get; set; }
        public int IdEmployee { get; set; }
        public int IdProfileChangeRequestType { get; set; }
        public string EntityCode { get; set; }
        public int? RecordId { get; set; }
        public string? Reason { get; set; }
        public string RequestStatus { get; set; }
        public int? CurrentLevel { get; set; }
        public DateTime? SubmittedDate { get; set; }
        public DateTime? FinalActionDate { get; set; }
        public int? FinalActionBy { get; set; }
        public DateTime? AppliedDate { get; set; }
        public string? ApproverRemarks { get; set; }
        public string? EmployeeResponse { get; set; }
        public int CreatedBy { get; set; }
        public DateTime CreatedDateTime { get; set; }
        public int? ModifiedBy { get; set; }
        public DateTime? ModifiedDateTime { get; set; }
    }

    // One row per field changed in a request.
    public class ProfileChangeRequestDetails
    {
        [Key]
        public int IdProfileChangeRequestDetail { get; set; }
        public int IdProfileChangeRequest { get; set; }
        public string TableName { get; set; }
        public string ColumnName { get; set; }
        public int? RecordId { get; set; }
        public string Operation { get; set; }
        public string? OldValue { get; set; }
        public string? NewValue { get; set; }
        public bool IsEncrypted { get; set; }
        public string? OldValueDisplay { get; set; }
        public string? NewValueDisplay { get; set; }
    }

    // Supporting documents uploaded with a request (stored outside wwwroot).
    public class ProfileChangeRequestDocuments
    {
        [Key]
        public int IdProfileChangeRequestDocument { get; set; }
        public int IdProfileChangeRequest { get; set; }
        public int? IdDocumentType { get; set; }
        public string OriginalFileName { get; set; }
        public string StoredFileName { get; set; }
        public string? MimeType { get; set; }
        public long FileSize { get; set; }
        public int UploadedBy { get; set; }
        public DateTime UploadedOn { get; set; }
        public bool IsDeleted { get; set; }
        public int? IdEmployeeDocument { get; set; }
    }
}
