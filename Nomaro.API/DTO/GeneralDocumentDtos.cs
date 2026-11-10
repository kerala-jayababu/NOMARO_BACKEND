namespace Nomaro.API.DTO
{
    public class GeneralDocumentTypeDto
    {
        public int IdGeneralDocumentType { get; set; }
        public string TypeCode { get; set; }
        public string TypeName { get; set; }
        public string? Description { get; set; }
        public int DisplayOrder { get; set; }
        public bool IsActive { get; set; } = true;
        public int SubtypeCount { get; set; }
        public int DocumentCount { get; set; }
    }

    public class GeneralDocumentSubtypeDto
    {
        public int IdGeneralDocumentSubtype { get; set; }
        public int IdGeneralDocumentType { get; set; }
        public string? TypeName { get; set; }
        public string SubtypeCode { get; set; }
        public string SubtypeName { get; set; }
        public string? Description { get; set; }
        public string DefaultMaintenanceType { get; set; } = "FIXED";
        public string DefaultScope { get; set; } = "ORGANIZATION";
        public string? PeriodFrequency { get; set; }
        public bool ValidityRequired { get; set; }
        public int ExpiryAlertDays { get; set; } = 30;
        public bool VisibleToAllEmployees { get; set; }
        public bool AcknowledgementRequired { get; set; }
        public int DisplayOrder { get; set; }
        public bool IsActive { get; set; } = true;
        public int DocumentCount { get; set; }
    }

    public class GeneralDocumentOfficeDto
    {
        public int IdOffice { get; set; }
        public string? OfficeCode { get; set; }
        public string? OfficeName { get; set; }
    }

    public class GeneralDocumentLookupsDto
    {
        public List<GeneralDocumentTypeDto> Types { get; set; } = new();
        public List<GeneralDocumentSubtypeDto> Subtypes { get; set; } = new();
        public List<GeneralDocumentOfficeDto> Offices { get; set; } = new();
        public bool AllOffices { get; set; }
        // permissions of the logged-in user, so the UI can hide buttons
        public bool CanRegister { get; set; }
        public bool CanEdit { get; set; }
        public bool CanUpload { get; set; }
        public bool CanMarkStatus { get; set; }
        public long MaxFileSizeBytes { get; set; }
        public int MaxFilesPerUpload { get; set; }
        public List<string> AllowedExtensions { get; set; } = new();
    }

    public class GeneralDocumentFilterDto
    {
        public int? IdGeneralDocumentType { get; set; }
        public int? IdGeneralDocumentSubtype { get; set; }
        public string? Scope { get; set; }
        public int? IdOffice { get; set; }
        public string? MaintenanceType { get; set; }
        // VALID | EXPIRING | EXPIRED | NO_EXPIRY | NO_RECORD | ATTENTION (expired + expiring + no record)
        public string? Validity { get; set; }
        // overrides each document's ExpiryAlertDays when given (Expiry Monitoring)
        public int? WithinDays { get; set; }
        public string? Search { get; set; }
        public bool IncludeInactive { get; set; }
    }

    public class GeneralDocumentListDto
    {
        public int IdGeneralDocument { get; set; }
        public string? DocumentCode { get; set; }
        public string Title { get; set; }
        public int IdGeneralDocumentType { get; set; }
        public string? TypeName { get; set; }
        public int? IdGeneralDocumentSubtype { get; set; }
        public string? SubtypeName { get; set; }
        public string Scope { get; set; }
        public int? IdOffice { get; set; }
        public string? OfficeName { get; set; }
        public string MaintenanceType { get; set; }
        public string? PeriodFrequency { get; set; }
        public bool ValidityRequired { get; set; }
        public int ExpiryAlertDays { get; set; }
        public string? Description { get; set; }
        public string? Tags { get; set; }
        public bool IsActive { get; set; }
        public int RecordCount { get; set; }
        // current record
        public int? IdCurrentRecord { get; set; }
        public int? VersionNumber { get; set; }
        public string? PeriodLabel { get; set; }
        public DateTime? PeriodTo { get; set; }
        public string? ReferenceNumber { get; set; }
        public DateTime? ValidFrom { get; set; }
        public DateTime? ValidTo { get; set; }
        public DateTime? LastUploadedDateTime { get; set; }
        public int FileCount { get; set; }
        public string ValidityStatus { get; set; }   // VALID | EXPIRING | EXPIRED | NO_EXPIRY | NO_RECORD
        public int? DaysToExpiry { get; set; }
    }

    public class GeneralDocumentSummaryDto
    {
        public int Total { get; set; }
        public int Valid { get; set; }
        public int NoExpiry { get; set; }
        public int Expiring { get; set; }
        public int Expired { get; set; }
        public int NoRecord { get; set; }
    }

    public class SaveGeneralDocumentDto
    {
        public int IdGeneralDocument { get; set; }
        public string Title { get; set; }
        public int IdGeneralDocumentType { get; set; }
        public int? IdGeneralDocumentSubtype { get; set; }
        public string Scope { get; set; }
        public int? IdOffice { get; set; }
        public string MaintenanceType { get; set; }
        public string? PeriodFrequency { get; set; }
        public bool ValidityRequired { get; set; }
        public int ExpiryAlertDays { get; set; } = 30;
        public string? Description { get; set; }
        public string? Tags { get; set; }
    }

    public class UploadGeneralDocumentRecordDto
    {
        public int IdGeneralDocument { get; set; }
        public string? PeriodLabel { get; set; }
        public DateTime? PeriodFrom { get; set; }
        public DateTime? PeriodTo { get; set; }
        public string? ReferenceNumber { get; set; }
        public string? IssuingAuthority { get; set; }
        public DateTime? IssueDate { get; set; }
        public DateTime? ValidFrom { get; set; }
        public DateTime? ValidTo { get; set; }
        public string? Remarks { get; set; }
        public List<IFormFile>? Files { get; set; }
    }

    public class UpdateGeneralDocumentRecordDto
    {
        public int IdGeneralDocumentRecord { get; set; }
        public string? PeriodLabel { get; set; }
        public DateTime? PeriodFrom { get; set; }
        public DateTime? PeriodTo { get; set; }
        public string? ReferenceNumber { get; set; }
        public string? IssuingAuthority { get; set; }
        public DateTime? IssueDate { get; set; }
        public DateTime? ValidFrom { get; set; }
        public DateTime? ValidTo { get; set; }
        public string? Remarks { get; set; }
    }

    public class GeneralDocumentActiveDto
    {
        public int IdGeneralDocument { get; set; }
        public bool IsActive { get; set; }
        public string? Remarks { get; set; }
    }

    public class GeneralDocumentRecordStatusDto
    {
        public int IdGeneralDocumentRecord { get; set; }
        public string Status { get; set; }   // INCORRECT | SUPERSEDED
        public string? Remarks { get; set; }
    }

    public class GeneralDocumentFileDto
    {
        public int IdGeneralDocumentFile { get; set; }
        public string OriginalFileName { get; set; }
        public string? MimeType { get; set; }
        public long FileSize { get; set; }
        public DateTime UploadedDateTime { get; set; }
    }

    public class GeneralDocumentRecordDto
    {
        public int IdGeneralDocumentRecord { get; set; }
        public int VersionNumber { get; set; }
        public string? PeriodLabel { get; set; }
        public DateTime? PeriodFrom { get; set; }
        public DateTime? PeriodTo { get; set; }
        public string? ReferenceNumber { get; set; }
        public string? IssuingAuthority { get; set; }
        public DateTime? IssueDate { get; set; }
        public DateTime? ValidFrom { get; set; }
        public DateTime? ValidTo { get; set; }
        public string? Remarks { get; set; }
        public string RecordStatus { get; set; }
        public bool IsCurrent { get; set; }
        public string? StatusRemarks { get; set; }
        public string? StatusChangedByName { get; set; }
        public DateTime? StatusChangedDateTime { get; set; }
        public int? SupersededByVersion { get; set; }
        public string? UploadedByName { get; set; }
        public DateTime UploadedDateTime { get; set; }
        public string? ValidityStatus { get; set; }
        public List<GeneralDocumentFileDto> Files { get; set; } = new();
    }

    public class GeneralDocumentAuditDto
    {
        public string Action { get; set; }
        public int? VersionNumber { get; set; }
        public string? Details { get; set; }
        public string? ActionByName { get; set; }
        public DateTime ActionDateTime { get; set; }
    }

    public class GeneralDocumentDetailDto
    {
        public GeneralDocumentListDto Document { get; set; } = new();
        public List<GeneralDocumentRecordDto> Records { get; set; } = new();
        public List<GeneralDocumentAuditDto> Audit { get; set; } = new();
        public bool CanManage { get; set; }   // user may upload / edit this document (office access)
    }

    public class GeneralDocumentZipRequestDto
    {
        public List<int>? DocumentIds { get; set; }   // current record files of each document
        public List<int>? RecordIds { get; set; }     // files of specific records (history)
    }

    /// <summary>Permissions of the caller, resolved by the controller.</summary>
    public class GeneralDocumentUserContext
    {
        public int IdEmployee { get; set; }
        public bool CanView { get; set; }
        public bool CanRegister { get; set; }
        public bool CanEdit { get; set; }
        public bool CanUpload { get; set; }
        public bool CanMarkStatus { get; set; }
        public bool CanReport { get; set; }
        public bool CanViewAck { get; set; }
        public bool CanSendAckReminder { get; set; }
        public bool AllOfficesPermission { get; set; }
        public string? IPAddress { get; set; }
    }

    public class GeneralDocumentReportFilterDto
    {
        public string ReportCode { get; set; }        // EXPIRY | MISSING | COMPLIANCE | ACTIVITY
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public int? IdGeneralDocumentType { get; set; }
        public int? IdGeneralDocumentSubtype { get; set; }
        public string? Scope { get; set; }
        public int? IdOffice { get; set; }
        public int? WithinDays { get; set; }          // compliance: expiring window (null = each document's alert days)
        public string? GroupBy { get; set; }          // compliance: OFFICE | TYPE
        public string? Action { get; set; }           // activity: one audit action, or null for all
        public bool IncludeViews { get; set; }        // activity: include VIEW / DOWNLOAD entries
    }

    public class GeneralDocumentReportColumnDto
    {
        public string Key { get; set; }
        public string Label { get; set; }
        public string Type { get; set; } = "text";    // text | date | datetime | number | percent
    }

    public class GeneralDocumentReportDto
    {
        public string ReportCode { get; set; }
        public string Title { get; set; }
        public string? Criteria { get; set; }
        public List<GeneralDocumentReportColumnDto> Columns { get; set; } = new();
        public List<Dictionary<string, object?>> Rows { get; set; } = new();
        public List<KeyValuePair<string, string>> Summary { get; set; } = new();
        public bool Truncated { get; set; }
    }

    // ---------- Self Portal: Documents ----------

    public class MyPublicDocumentDto
    {
        public int IdGeneralDocument { get; set; }
        public int IdGeneralDocumentRecord { get; set; }
        public string? DocumentCode { get; set; }
        public string Title { get; set; }
        public string? Description { get; set; }
        public string? TypeName { get; set; }
        public string? SubtypeName { get; set; }
        public string? OfficeName { get; set; }      // null = organisation
        public int VersionNumber { get; set; }
        public string? PeriodLabel { get; set; }
        public DateTime? ValidFrom { get; set; }
        public DateTime? ValidTo { get; set; }
        public DateTime PublishedDateTime { get; set; }
        public bool AcknowledgementRequired { get; set; }
        public DateTime? FirstViewedDateTime { get; set; }
        public DateTime? AcknowledgedDateTime { get; set; }
        public List<GeneralDocumentFileDto> Files { get; set; } = new();
    }

    // ---------- Document Ack Dashboard ----------

    public class AckDocumentSummaryDto
    {
        public int IdGeneralDocument { get; set; }
        public int IdGeneralDocumentRecord { get; set; }
        public string? DocumentCode { get; set; }
        public string Title { get; set; }
        public string? TypeName { get; set; }
        public string? SubtypeName { get; set; }
        public string? OfficeName { get; set; }
        public int VersionNumber { get; set; }
        public string? PeriodLabel { get; set; }
        public DateTime PublishedDateTime { get; set; }
        public bool AcknowledgementRequired { get; set; }
        public int Audience { get; set; }           // working employees who should see it
        public int Acknowledged { get; set; }
        public int ViewedOnly { get; set; }         // viewed but not acknowledged
        public int NotViewed { get; set; }
        public decimal AcknowledgedPercent { get; set; }
        public decimal ViewedPercent { get; set; }
    }

    public class AckDashboardDto
    {
        public List<AckDocumentSummaryDto> Documents { get; set; } = new();
        // organisation totals over documents that need acknowledgement
        public int TotalExpected { get; set; }
        public int TotalAcknowledged { get; set; }
        public int TotalViewedOnly { get; set; }
        public int TotalNotViewed { get; set; }
        public decimal AcknowledgedPercent { get; set; }
        public bool CanSendReminder { get; set; }
    }

    public class AckEmployeeDto
    {
        public int IdEmployee { get; set; }
        public string? EmployeeCode { get; set; }
        public string? EmployeeName { get; set; }
        public string? DepartmentName { get; set; }
        public string? DesignationName { get; set; }
        public string? OfficeName { get; set; }
        public string? Email { get; set; }
        public string Status { get; set; }           // ACKNOWLEDGED | VIEWED | NOT_VIEWED
        public DateTime? FirstViewedDateTime { get; set; }
        public DateTime? AcknowledgedDateTime { get; set; }
    }

    public class AckReminderRequestDto
    {
        public int? IdGeneralDocument { get; set; }   // null = every document that needs acknowledgement
        public List<int>? EmployeeIds { get; set; }   // null = everyone who has not acknowledged
    }

    public class AckReminderResultDto
    {
        public int Employees { get; set; }
        public int Sent { get; set; }
        public int Failed { get; set; }
        public int NoEmail { get; set; }
        public List<string> NoEmailEmployees { get; set; } = new();
    }
}
