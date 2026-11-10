using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Nomaro.API.Models
{
    /// <summary>Document Management master: document type (separate from the employee DocumentTypes table).</summary>
    [Table("GeneralDocumentTypes")]
    public class GeneralDocumentTypes
    {
        [Key]
        public int IdGeneralDocumentType { get; set; }
        public string TypeCode { get; set; }
        public string TypeName { get; set; }
        public string? Description { get; set; }
        public int DisplayOrder { get; set; }
        public bool IsActive { get; set; }
        public int? CreatedBy { get; set; }
        public DateTime CreatedDateTime { get; set; }
        public int? ModifiedBy { get; set; }
        public DateTime? ModifiedDateTime { get; set; }
    }

    [Table("GeneralDocumentSubtypes")]
    public class GeneralDocumentSubtypes
    {
        [Key]
        public int IdGeneralDocumentSubtype { get; set; }
        public int IdGeneralDocumentType { get; set; }
        public string SubtypeCode { get; set; }
        public string SubtypeName { get; set; }
        public string? Description { get; set; }
        public string DefaultMaintenanceType { get; set; }   // FIXED | PERIODIC | VERSION
        public string DefaultScope { get; set; }             // ORGANIZATION | OFFICE
        public string? PeriodFrequency { get; set; }
        public bool ValidityRequired { get; set; }
        public int ExpiryAlertDays { get; set; }
        public bool VisibleToAllEmployees { get; set; }      // shown to employees in Self Portal -> Documents
        public bool AcknowledgementRequired { get; set; }    // employees must click "Viewed and Acknowledged"
        public int DisplayOrder { get; set; }
        public bool IsActive { get; set; }
        public int? CreatedBy { get; set; }
        public DateTime CreatedDateTime { get; set; }
        public int? ModifiedBy { get; set; }
        public DateTime? ModifiedDateTime { get; set; }
    }

    /// <summary>A logical document (e.g. "Trade Licence - Kochi office"). Its uploads are GeneralDocumentRecords.</summary>
    [Table("GeneralDocuments")]
    public class GeneralDocuments
    {
        [Key]
        public int IdGeneralDocument { get; set; }
        public string? DocumentCode { get; set; }
        public string Title { get; set; }
        public int IdGeneralDocumentType { get; set; }
        public int? IdGeneralDocumentSubtype { get; set; }
        public string Scope { get; set; }                    // ORGANIZATION | OFFICE
        public int? IdOffice { get; set; }
        public string MaintenanceType { get; set; }          // FIXED | PERIODIC | VERSION
        public string? PeriodFrequency { get; set; }
        public bool ValidityRequired { get; set; }
        public int ExpiryAlertDays { get; set; }
        public string? Description { get; set; }
        public string? Tags { get; set; }
        public bool IsActive { get; set; }
        public int? CreatedBy { get; set; }
        public DateTime CreatedDateTime { get; set; }
        public int? ModifiedBy { get; set; }
        public DateTime? ModifiedDateTime { get; set; }
    }

    [Table("GeneralDocumentRecords")]
    public class GeneralDocumentRecords
    {
        [Key]
        public int IdGeneralDocumentRecord { get; set; }
        public int IdGeneralDocument { get; set; }
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
        public string RecordStatus { get; set; }             // ACTIVE | SUPERSEDED | INCORRECT
        public bool IsCurrent { get; set; }
        public string? StatusRemarks { get; set; }
        public int? StatusChangedBy { get; set; }
        public DateTime? StatusChangedDateTime { get; set; }
        public int? SupersededByIdRecord { get; set; }
        public int? UploadedBy { get; set; }
        public DateTime UploadedDateTime { get; set; }
    }

    [Table("GeneralDocumentFiles")]
    public class GeneralDocumentFiles
    {
        [Key]
        public int IdGeneralDocumentFile { get; set; }
        public int IdGeneralDocumentRecord { get; set; }
        public string OriginalFileName { get; set; }
        public string StoredFileName { get; set; }
        public string? MimeType { get; set; }
        public long FileSize { get; set; }
        public string? FileHash { get; set; }
        public int? UploadedBy { get; set; }
        public DateTime UploadedDateTime { get; set; }
    }

    /// <summary>Insert-only trail (a DB trigger blocks update / delete).</summary>
    [Table("GeneralDocumentAudit")]
    public class GeneralDocumentAudit
    {
        [Key]
        public long IdGeneralDocumentAudit { get; set; }
        public int? IdGeneralDocument { get; set; }
        public int? IdGeneralDocumentRecord { get; set; }
        public int? IdGeneralDocumentFile { get; set; }
        public string Action { get; set; }
        public string? Details { get; set; }
        public int? ActionBy { get; set; }
        public DateTime ActionDateTime { get; set; }
        public string? IPAddress { get; set; }
    }

    /// <summary>One row per employee per document version: when they viewed it and when they acknowledged it.</summary>
    [Table("GeneralDocumentAcknowledgements")]
    public class GeneralDocumentAcknowledgements
    {
        [Key]
        public int IdGeneralDocumentAcknowledgement { get; set; }
        public int IdGeneralDocument { get; set; }
        public int IdGeneralDocumentRecord { get; set; }
        public int IdEmployee { get; set; }
        public DateTime? FirstViewedDateTime { get; set; }
        public DateTime? LastViewedDateTime { get; set; }
        public int ViewCount { get; set; }
        public DateTime? AcknowledgedDateTime { get; set; }
        public string? IPAddress { get; set; }
        public string? UserAgent { get; set; }
    }
}
