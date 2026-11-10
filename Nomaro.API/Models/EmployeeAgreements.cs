using System.ComponentModel.DataAnnotations;

namespace Nomaro.API.Models
{
    // A policy / agreement document sent to an employee for electronic acceptance.
    public class EmployeeAgreements
    {
        [Key]
        public int IdEmployeeAgreement { get; set; }
        public int IdEmployee { get; set; }
        public int? IdDocumentType { get; set; }
        public string AgreementTitle { get; set; }
        public string? AgreementVersion { get; set; }
        public string OriginalFileName { get; set; }
        public string StoredFileName { get; set; }
        public long FileSize { get; set; }
        public string FileHash { get; set; }              // SHA-256 of the exact PDF sent
        public string Status { get; set; }                // SENT | VIEWED | ACCEPTED | REJECTED | CANCELLED
        public string? SentTo { get; set; }
        public DateTime? SentOnUtc { get; set; }
        public int? SentBy { get; set; }
        public DateTime? LinkExpiresOnUtc { get; set; }
        public DateTime? ViewedOnUtc { get; set; }
        public DateTime? RespondedOnUtc { get; set; }
        public string? ResponseName { get; set; }
        public string? ResponseRemarks { get; set; }
        public string? ResponseIPAddress { get; set; }
        public string? ResponseUserAgent { get; set; }
        public string? CertificateFileName { get; set; }
        public string? CertificateHash { get; set; }
        public string? Remarks { get; set; }
        public string? CancelReason { get; set; }
        public int CreatedBy { get; set; }
        public DateTime CreatedDateTime { get; set; }
        public int? ModifiedBy { get; set; }
        public DateTime? ModifiedDateTime { get; set; }
    }

    // Link token + email one-time code. Only hashes are stored.
    public class EmployeeAgreementTokens
    {
        [Key]
        public int IdEmployeeAgreementToken { get; set; }
        public int IdEmployeeAgreement { get; set; }
        public string TokenHash { get; set; }
        public DateTime CreatedOnUtc { get; set; }
        public DateTime ExpiresOnUtc { get; set; }
        public bool IsRevoked { get; set; }
        public string? OtpHash { get; set; }
        public DateTime? OtpExpiresOnUtc { get; set; }
        public int OtpAttempts { get; set; }
        public int OtpSentCount { get; set; }
        public DateTime? OtpSentOnUtc { get; set; }
    }

    // Append-only evidence trail. Each row carries the hash of the previous row, so a change or deletion is detectable.
    public class EmployeeAgreementEvents
    {
        [Key]
        public long IdEmployeeAgreementEvent { get; set; }
        public int IdEmployeeAgreement { get; set; }
        public string EventType { get; set; }
        public DateTime EventTimeUtc { get; set; }
        public string ActorType { get; set; }             // HR | EMPLOYEE | SYSTEM
        public int? ActorId { get; set; }
        public string? IPAddress { get; set; }
        public string? UserAgent { get; set; }
        public string? Details { get; set; }
        public string PrevHash { get; set; }
        public string EventHash { get; set; }
    }
}
