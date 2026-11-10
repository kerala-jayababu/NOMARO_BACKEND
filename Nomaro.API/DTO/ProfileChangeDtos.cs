namespace Nomaro.API.DTO
{
    public class LookupOptionDto
    {
        public string Value { get; set; }
        public string Label { get; set; }
        public string? ParentValue { get; set; }
    }

    public class ProfileChangeFieldDto
    {
        public string Column { get; set; }
        public string Label { get; set; }
        public string DataType { get; set; }
        public bool Required { get; set; }
        public int MaxLength { get; set; }
        public bool IsSensitive { get; set; }
        public List<LookupOptionDto>? Options { get; set; }
    }

    public class ProfileChangeRequestTypeDto
    {
        public int IdProfileChangeRequestType { get; set; }
        public string RequestCategory { get; set; }
        public string RequestTypeCode { get; set; }
        public string RequestTypeName { get; set; }
        public string EntityCode { get; set; }
        public bool ProofRequired { get; set; }
        public string? ProofHint { get; set; }
        public int DisplayOrder { get; set; }
        public bool IsActive { get; set; }
        public string TableName { get; set; }
        public string Operation { get; set; }
        public bool NeedsRecord { get; set; }
        public List<ProfileChangeFieldDto> Fields { get; set; } = new();
    }

    public class ProfileChangeLookupsDto
    {
        public List<ProfileChangeRequestTypeDto> RequestTypes { get; set; } = new();
        public List<LookupOptionDto> Banks { get; set; } = new();
        public List<LookupOptionDto> Branches { get; set; } = new();
        public List<LookupOptionDto> DocumentTypes { get; set; } = new();
    }

    public class MyProfileEmployeeDto
    {
        public int IdEmployee { get; set; }
        public string? EmployeeCode { get; set; }
        public string? FullName { get; set; }
        public string? DepartmentName { get; set; }
        public string? DesignationName { get; set; }
        public string? GradeName { get; set; }
        public string? ReportingToName { get; set; }
        public DateTime? JoiningDate { get; set; }
        public string? OfficialEmail { get; set; }
        public string? PhotoBase64 { get; set; }
    }

    public class MyBankAccountDto
    {
        public int IdEmployeeBankAccount { get; set; }
        public int IdBank { get; set; }
        public string? BankName { get; set; }
        public int? IdBankBranch { get; set; }
        public string? BranchName { get; set; }
        public string? AccountNumberMasked { get; set; }
        public string? BranchCode { get; set; }
        public string? CurrencyCode { get; set; }
        public decimal SalaryPercentageDistributed { get; set; }
    }

    public class MyDocumentDto
    {
        public int IdEmployeeDocument { get; set; }
        public int IdDocumentType { get; set; }
        public string? DocumentTypeName { get; set; }
        public DateTime? DocumentValidTill { get; set; }
        public string? Remarks { get; set; }
        public bool HasFile { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class PendingFieldDto
    {
        public string TableName { get; set; }
        public string ColumnName { get; set; }
        public int? RecordId { get; set; }
        public int IdProfileChangeRequest { get; set; }
        public string? RequestNumber { get; set; }
        public string RequestStatus { get; set; }
    }

    public class MyProfileDto
    {
        public MyProfileEmployeeDto Employee { get; set; } = new();
        // Display values keyed "Table.Column" (sensitive values masked)
        public Dictionary<string, string?> Values { get; set; } = new();
        public List<EmployeeFamilyMemberDto> FamilyMembers { get; set; } = new();
        public List<MyBankAccountDto> BankAccounts { get; set; } = new();
        public List<MyDocumentDto> Documents { get; set; } = new();
        public List<PendingFieldDto> PendingFields { get; set; } = new();
    }

    public class SaveProfileChangeRequestDto
    {
        public int? IdProfileChangeRequest { get; set; }
        public string RequestTypeCode { get; set; }
        public int? RecordId { get; set; }
        public string? Reason { get; set; }
        public string? EmployeeResponse { get; set; }
        // JSON object: { "Column": "new value", ... }
        public string? ValuesJson { get; set; }
        public List<int>? RemoveDocumentIds { get; set; }
        public List<IFormFile>? Files { get; set; }
        public List<int>? FileDocumentTypeIds { get; set; }
    }

    public class ProfileChangeRequestListDto
    {
        public int IdProfileChangeRequest { get; set; }
        public string? RequestNumber { get; set; }
        public int IdEmployee { get; set; }
        public string? EmployeeCode { get; set; }
        public string? EmployeeName { get; set; }
        public string? DepartmentName { get; set; }
        public string? DesignationName { get; set; }
        public string RequestCategory { get; set; }
        public string RequestTypeName { get; set; }
        public string RequestStatus { get; set; }
        public int? CurrentLevel { get; set; }
        public int TotalLevels { get; set; }
        public DateTime? SubmittedDate { get; set; }
        public DateTime CreatedDateTime { get; set; }
        public DateTime? FinalActionDate { get; set; }
        public int? WaitingDays { get; set; }
        public bool IsResubmitted { get; set; }
    }

    public class ProfileChangeRowDto
    {
        public int IdProfileChangeRequestDetail { get; set; }
        public string ColumnName { get; set; }
        public string Label { get; set; }
        public string Operation { get; set; }
        public string? OldValue { get; set; }
        public string? NewValue { get; set; }
        public string? CurrentValue { get; set; }
        public bool HasConflict { get; set; }
        public bool IsSensitive { get; set; }
        // Unmasked new value: only for the owner while the request can be edited
        public string? NewValueRaw { get; set; }
    }

    public class ProfileChangeDocumentDto
    {
        public int IdProfileChangeRequestDocument { get; set; }
        public int? IdDocumentType { get; set; }
        public string? DocumentTypeName { get; set; }
        public string OriginalFileName { get; set; }
        public string? MimeType { get; set; }
        public long FileSize { get; set; }
        public DateTime UploadedOn { get; set; }
    }

    public class ProfileChangeHistoryDto
    {
        public DateTime? ActionDate { get; set; }
        public string Action { get; set; }
        public string? ActionBy { get; set; }
        public int? LevelNumber { get; set; }
        public int? CycleIndex { get; set; }
        public string? Remarks { get; set; }
        public bool IsPending { get; set; }
    }

    public class ProfileChangeRequestDetailDto
    {
        public int IdProfileChangeRequest { get; set; }
        public string? RequestNumber { get; set; }
        public string RequestTypeCode { get; set; }
        public string RequestTypeName { get; set; }
        public string RequestCategory { get; set; }
        public string EntityCode { get; set; }
        public string TableName { get; set; }
        public string Operation { get; set; }
        public bool ProofRequired { get; set; }
        public string? ProofHint { get; set; }
        public int? RecordId { get; set; }
        public string? RecordLabel { get; set; }
        public string RequestStatus { get; set; }
        public int? CurrentLevel { get; set; }
        public int TotalLevels { get; set; }
        public string? Reason { get; set; }
        public string? ApproverRemarks { get; set; }
        public string? EmployeeResponse { get; set; }
        public DateTime CreatedDateTime { get; set; }
        public DateTime? SubmittedDate { get; set; }
        public DateTime? FinalActionDate { get; set; }
        public MyProfileEmployeeDto Employee { get; set; } = new();
        public List<ProfileChangeRowDto> Changes { get; set; } = new();
        public List<ProfileChangeDocumentDto> Documents { get; set; } = new();
        public List<ProfileChangeHistoryDto> History { get; set; } = new();
        public bool CanEdit { get; set; }
        public bool CanCancel { get; set; }
        public bool CanAct { get; set; }
        public bool IsFinalLevel { get; set; }
        public bool HasConflicts { get; set; }
        public bool CanRevealSensitive { get; set; }
    }

    public class ProfileChangeActionDto
    {
        public int IdProfileChangeRequest { get; set; }
        public string Action { get; set; }   // APPROVE | REJECT | CLARIFY
        public string? Remarks { get; set; }
        public bool ConfirmConflicts { get; set; }
    }

    public class ProfileChangeDashboardDto
    {
        public int WaitingForMe { get; set; }
        public int WithEmployee { get; set; }
        public int Approved30Days { get; set; }
        public int Rejected30Days { get; set; }
    }

    public class ProfileChangeHrFilterDto
    {
        public string Mode { get; set; } = "MINE";   // MINE | ALL
        public string? Status { get; set; }
        public string? Category { get; set; }
        public int? IdDepartment { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public string? Search { get; set; }
    }

    public class ProfileChangeRequestTypeUpdateDto
    {
        public int IdProfileChangeRequestType { get; set; }
        public string RequestTypeName { get; set; }
        public bool ProofRequired { get; set; }
        public string? ProofHint { get; set; }
        public int DisplayOrder { get; set; }
        public bool IsActive { get; set; }
    }

    public class ProfileChangeWorkflowLevelDto
    {
        public int LevelNumber { get; set; }
        public string ApprovalAuthorityType { get; set; }   // ROLE (designation) | REPOFFICER | EMPLOYEE
        public int? ApprovalAuthorityID { get; set; }
        public string? AuthorityName { get; set; }
    }

    public class ProfileChangeWorkflowDto
    {
        public string EntityCode { get; set; }
        public string EntityName { get; set; }
        public List<ProfileChangeWorkflowLevelDto> Levels { get; set; } = new();
    }

    public class ProfileChangeHistoryEntryDto
    {
        public DateTime ActionTimestamp { get; set; }
        public string? ActionBy { get; set; }
        public string? Details { get; set; }
    }

    public class ProfileChangeWorkflowLookupsDto
    {
        public List<LookupOptionDto> Designations { get; set; } = new();
        public List<LookupOptionDto> Employees { get; set; } = new();
    }

    public class FileDownloadDto
    {
        public byte[] Content { get; set; }
        public string ContentType { get; set; }
        public string FileName { get; set; }
    }
}
