namespace Nomaro.API.DTO
{
    public class SendEmployeeAgreementDto
    {
        public int IdEmployee { get; set; }
        public string AgreementTitle { get; set; }
        public string? AgreementVersion { get; set; }
        public int? IdDocumentType { get; set; }
        public string? Remarks { get; set; }
        public IFormFile? File { get; set; }
    }

    public class EmployeeAgreementFilterDto
    {
        public int? IdEmployee { get; set; }
        public string? Status { get; set; }
        public string? Search { get; set; }
    }

    public class EmployeeAgreementListDto
    {
        public int IdEmployeeAgreement { get; set; }
        public int IdEmployee { get; set; }
        public string? EmployeeCode { get; set; }
        public string? EmployeeName { get; set; }
        public string? DepartmentName { get; set; }
        public string AgreementTitle { get; set; }
        public string? AgreementVersion { get; set; }
        public string? DocumentTypeName { get; set; }
        public string OriginalFileName { get; set; }
        public string Status { get; set; }          // EXPIRED is worked out from the link expiry
        public string? SentTo { get; set; }
        public DateTime? SentOnUtc { get; set; }
        public DateTime? LinkExpiresOnUtc { get; set; }
        public DateTime? ViewedOnUtc { get; set; }
        public DateTime? RespondedOnUtc { get; set; }
        public bool HasCertificate { get; set; }
    }

    public class EmployeeAgreementEventDto
    {
        public string EventType { get; set; }
        public DateTime EventTimeUtc { get; set; }
        public string ActorType { get; set; }
        public string? ActorName { get; set; }
        public string? IPAddress { get; set; }
        public string? UserAgent { get; set; }
        public string? Details { get; set; }
        public string EventHash { get; set; }
    }

    public class EmployeeAgreementDetailDto : EmployeeAgreementListDto
    {
        public string FileHash { get; set; }
        public long FileSize { get; set; }
        public string? Remarks { get; set; }
        public string? ResponseName { get; set; }
        public string? ResponseRemarks { get; set; }
        public string? ResponseIPAddress { get; set; }
        public string? ResponseUserAgent { get; set; }
        public string? CertificateHash { get; set; }
        public string? CancelReason { get; set; }
        public string? SentByName { get; set; }
        public bool TrailIntact { get; set; }
        public List<EmployeeAgreementEventDto> Events { get; set; } = new();
    }

    public class AgreementEmployeeOptionDto
    {
        public int IdEmployee { get; set; }
        public string? EmployeeCode { get; set; }
        public string? EmployeeName { get; set; }
        public string? OfficialEmail { get; set; }
        public string? PersonalEmail { get; set; }
        public string? CurrentStatus { get; set; }
    }

    // ---------- Public review page ----------

    public class AgreementPortalViewDto
    {
        public string State { get; set; }           // OPEN | ACCEPTED | REJECTED | EXPIRED | CANCELLED | INVALID
        public string? CompanyName { get; set; }
        public string? EmployeeName { get; set; }
        public string? AgreementTitle { get; set; }
        public string? AgreementVersion { get; set; }
        public string? OriginalFileName { get; set; }
        public string? FileHash { get; set; }
        public DateTime? SentOnUtc { get; set; }
        public DateTime? LinkExpiresOnUtc { get; set; }
        public DateTime? RespondedOnUtc { get; set; }
        public List<string> MaskedEmails { get; set; } = new();
        public int OtpValidMinutes { get; set; }
    }

    public class AgreementTokenDto
    {
        public string Token { get; set; }
    }

    public class AgreementResponseDto
    {
        public string Token { get; set; }
        public string Decision { get; set; }        // ACCEPT | REJECT
        public string Otp { get; set; }
        public string FullName { get; set; }
        public bool ConfirmRead { get; set; }
        public string? Remarks { get; set; }
    }

    public record ClientInfo(string? IPAddress, string? UserAgent);
}
