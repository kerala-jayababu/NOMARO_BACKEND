using System.IO.Compression;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Nomaro.API.Database;
using Nomaro.API.DTO;
using Nomaro.API.Models;
using Nomaro.API.Services.Interface;

namespace Nomaro.API.Services.Implimentation
{
    /// <summary>
    /// General Document Management: organisation / office documents (licences, policies, agreements, returns ...).
    /// A logical document (GeneralDocuments) has one record per upload (GeneralDocumentRecords) and each record has files.
    ///   FIXED    - one valid document; a new upload replaces (supersedes) the previous one.
    ///   VERSION  - every upload is a new version; the previous version is superseded.
    ///   PERIODIC - one record per period; re-uploading the same period supersedes that period only.
    /// Office access: users see organisation documents and documents of their office and its child offices.
    /// Users with no office, or with "D" on the Document Register, see and manage all offices.
    /// </summary>
    public partial class GeneralDocumentService : IGeneralDocumentService
    {
        public static readonly string[] MaintenanceTypes = { "FIXED", "PERIODIC", "VERSION" };
        public static readonly string[] Scopes = { "ORGANIZATION", "OFFICE" };
        public static readonly string[] Frequencies = { "MONTHLY", "QUARTERLY", "HALFYEARLY", "YEARLY", "OTHER" };
        private static readonly string[] DefaultExtensions = { ".pdf", ".jpg", ".jpeg", ".png", ".doc", ".docx", ".xls", ".xlsx" };

        private const string Active = "ACTIVE", Superseded = "SUPERSEDED", Incorrect = "INCORRECT";

        private readonly ApplicationDBContext _db;
        private readonly IConfiguration _configuration;
        private readonly IWebHostEnvironment _env;
        private readonly ILogger<GeneralDocumentService> _logger;
        private readonly INotificationConfigService _notificationConfigService;

        public GeneralDocumentService(ApplicationDBContext db, IConfiguration configuration, IWebHostEnvironment env, ILogger<GeneralDocumentService> logger,
            INotificationConfigService notificationConfigService)
        {
            _notificationConfigService = notificationConfigService;
            _db = db;
            _configuration = configuration;
            _env = env;
            _logger = logger;
        }

        #region Settings

        private string StorageRoot
        {
            get
            {
                var path = _configuration["GeneralDocuments:StoragePath"];
                if (string.IsNullOrWhiteSpace(path))
                    path = Path.Combine(_env.ContentRootPath, "SecureUploads", "GeneralDocuments");
                return Path.GetFullPath(path);
            }
        }

        private long MaxFileBytes => (long)((_configuration.GetValue<double?>("GeneralDocuments:MaxFileSizeMB") ?? 10) * 1024 * 1024);
        private int MaxFiles => _configuration.GetValue<int?>("GeneralDocuments:MaxFilesPerUpload") ?? 5;
        private long MaxZipBytes => (long)((_configuration.GetValue<double?>("GeneralDocuments:MaxZipSizeMB") ?? 200) * 1024 * 1024);

        private string[] AllowedExtensions
        {
            get
            {
                var list = _configuration.GetSection("GeneralDocuments:AllowedExtensions").Get<string[]>();
                if (list == null || list.Length == 0) return DefaultExtensions;
                return list.Select(e => e.Trim().ToLowerInvariant()).Where(e => e.Length > 1).Select(e => e.StartsWith('.') ? e : "." + e).ToArray();
            }
        }

        #endregion

        public async Task<Dictionary<string, string>> GetScreenPermissions(int idEmployee, params string[] screenCodes)
        {
            var codes = screenCodes.Where(c => !string.IsNullOrWhiteSpace(c)).Distinct().ToList();
            var rows = await (from ep in _db.EmployeePermissions.AsNoTracking()
                              join ps in _db.PayrollScreens.AsNoTracking() on ep.IdPayrollScreen equals ps.IdPayrollScreen
                              where ep.IdEmployee == idEmployee && codes.Contains(ps.ScreenCode)
                              select new { ps.ScreenCode, ep.Permission }).ToListAsync();
            return rows.GroupBy(r => r.ScreenCode)
                .ToDictionary(g => g.Key, g => string.Concat(g.Select(r => r.Permission ?? "")), StringComparer.OrdinalIgnoreCase);
        }

        #region Masters

        public async Task<List<GeneralDocumentTypeDto>> GetTypes(bool includeInactive)
        {
            return await _db.GeneralDocumentTypes.AsNoTracking()
                .Where(t => includeInactive || t.IsActive)
                .OrderBy(t => t.DisplayOrder).ThenBy(t => t.TypeName)
                .Select(t => new GeneralDocumentTypeDto
                {
                    IdGeneralDocumentType = t.IdGeneralDocumentType,
                    TypeCode = t.TypeCode,
                    TypeName = t.TypeName,
                    Description = t.Description,
                    DisplayOrder = t.DisplayOrder,
                    IsActive = t.IsActive,
                    SubtypeCount = _db.GeneralDocumentSubtypes.Count(s => s.IdGeneralDocumentType == t.IdGeneralDocumentType),
                    DocumentCount = _db.GeneralDocuments.Count(d => d.IdGeneralDocumentType == t.IdGeneralDocumentType)
                })
                .ToListAsync();
        }

        public async Task<int> SaveType(GeneralDocumentTypeDto dto, int idEmployee)
        {
            if (dto == null) throw new InvalidOperationException("Enter the document type details.");
            var code = (dto.TypeCode ?? "").Trim().ToUpperInvariant();
            var name = (dto.TypeName ?? "").Trim();
            if (code.Length == 0) throw new InvalidOperationException("Enter the type code.");
            if (!Regex.IsMatch(code, "^[A-Z0-9_]{1,20}$")) throw new InvalidOperationException("Type code can have only letters, numbers and _ (up to 20).");
            if (name.Length == 0) throw new InvalidOperationException("Enter the type name.");
            if (name.Length > 100) throw new InvalidOperationException("Type name must not exceed 100 characters.");
            if (dto.Description?.Trim().Length > 250) throw new InvalidOperationException("Description must not exceed 250 characters.");

            if (await _db.GeneralDocumentTypes.AnyAsync(t => t.IdGeneralDocumentType != dto.IdGeneralDocumentType && t.TypeCode == code))
                throw new InvalidOperationException($"Type code {code} is already used.");
            if (await _db.GeneralDocumentTypes.AnyAsync(t => t.IdGeneralDocumentType != dto.IdGeneralDocumentType && t.TypeName == name))
                throw new InvalidOperationException($"Document type \"{name}\" already exists.");

            GeneralDocumentTypes entity;
            if (dto.IdGeneralDocumentType > 0)
            {
                entity = await _db.GeneralDocumentTypes.FirstOrDefaultAsync(t => t.IdGeneralDocumentType == dto.IdGeneralDocumentType)
                    ?? throw new KeyNotFoundException("Document type not found.");
                entity.ModifiedBy = idEmployee;
                entity.ModifiedDateTime = CompanyTime.Now;
            }
            else
            {
                entity = new GeneralDocumentTypes { CreatedBy = idEmployee, CreatedDateTime = CompanyTime.Now };
                _db.GeneralDocumentTypes.Add(entity);
            }
            entity.TypeCode = code;
            entity.TypeName = name;
            entity.Description = Clean(dto.Description);
            entity.DisplayOrder = dto.DisplayOrder;
            entity.IsActive = dto.IsActive;
            await _db.SaveChangesAsync();
            return entity.IdGeneralDocumentType;
        }

        public async Task<List<GeneralDocumentSubtypeDto>> GetSubtypes(int? idType, bool includeInactive)
        {
            var query =
                from s in _db.GeneralDocumentSubtypes.AsNoTracking()
                join t in _db.GeneralDocumentTypes.AsNoTracking() on s.IdGeneralDocumentType equals t.IdGeneralDocumentType
                where (includeInactive || (s.IsActive && t.IsActive)) && (idType == null || idType <= 0 || s.IdGeneralDocumentType == idType)
                orderby t.DisplayOrder, t.TypeName, s.DisplayOrder, s.SubtypeName
                select new GeneralDocumentSubtypeDto
                {
                    IdGeneralDocumentSubtype = s.IdGeneralDocumentSubtype,
                    IdGeneralDocumentType = s.IdGeneralDocumentType,
                    TypeName = t.TypeName,
                    SubtypeCode = s.SubtypeCode,
                    SubtypeName = s.SubtypeName,
                    Description = s.Description,
                    DefaultMaintenanceType = s.DefaultMaintenanceType,
                    DefaultScope = s.DefaultScope,
                    PeriodFrequency = s.PeriodFrequency,
                    ValidityRequired = s.ValidityRequired,
                    ExpiryAlertDays = s.ExpiryAlertDays,
                    VisibleToAllEmployees = s.VisibleToAllEmployees,
                    AcknowledgementRequired = s.AcknowledgementRequired,
                    DisplayOrder = s.DisplayOrder,
                    IsActive = s.IsActive,
                    DocumentCount = _db.GeneralDocuments.Count(d => d.IdGeneralDocumentSubtype == s.IdGeneralDocumentSubtype)
                };
            return await query.ToListAsync();
        }

        public async Task<int> SaveSubtype(GeneralDocumentSubtypeDto dto, int idEmployee)
        {
            if (dto == null) throw new InvalidOperationException("Enter the subtype details.");
            var code = (dto.SubtypeCode ?? "").Trim().ToUpperInvariant();
            var name = (dto.SubtypeName ?? "").Trim();
            var maint = (dto.DefaultMaintenanceType ?? "").Trim().ToUpperInvariant();
            var scope = (dto.DefaultScope ?? "").Trim().ToUpperInvariant();
            var freq = string.IsNullOrWhiteSpace(dto.PeriodFrequency) ? null : dto.PeriodFrequency.Trim().ToUpperInvariant();

            if (!await _db.GeneralDocumentTypes.AnyAsync(t => t.IdGeneralDocumentType == dto.IdGeneralDocumentType))
                throw new InvalidOperationException("Select the document type.");
            if (code.Length == 0) throw new InvalidOperationException("Enter the subtype code.");
            if (!Regex.IsMatch(code, "^[A-Z0-9_]{1,20}$")) throw new InvalidOperationException("Subtype code can have only letters, numbers and _ (up to 20).");
            if (name.Length == 0) throw new InvalidOperationException("Enter the subtype name.");
            if (name.Length > 100) throw new InvalidOperationException("Subtype name must not exceed 100 characters.");
            if (dto.Description?.Trim().Length > 250) throw new InvalidOperationException("Description must not exceed 250 characters.");
            if (!MaintenanceTypes.Contains(maint)) throw new InvalidOperationException("Select the maintenance type.");
            if (!Scopes.Contains(scope)) throw new InvalidOperationException("Select the scope.");
            if (maint != "PERIODIC") freq = null;
            else if (freq != null && !Frequencies.Contains(freq)) throw new InvalidOperationException("Select a valid period frequency.");
            if (dto.ExpiryAlertDays < 0 || dto.ExpiryAlertDays > 365) throw new InvalidOperationException("Expiry alert days must be between 0 and 365.");

            if (await _db.GeneralDocumentSubtypes.AnyAsync(s => s.IdGeneralDocumentSubtype != dto.IdGeneralDocumentSubtype && s.IdGeneralDocumentType == dto.IdGeneralDocumentType && s.SubtypeCode == code))
                throw new InvalidOperationException($"Subtype code {code} is already used in this type.");
            if (await _db.GeneralDocumentSubtypes.AnyAsync(s => s.IdGeneralDocumentSubtype != dto.IdGeneralDocumentSubtype && s.IdGeneralDocumentType == dto.IdGeneralDocumentType && s.SubtypeName == name))
                throw new InvalidOperationException($"Subtype \"{name}\" already exists in this type.");

            GeneralDocumentSubtypes entity;
            if (dto.IdGeneralDocumentSubtype > 0)
            {
                entity = await _db.GeneralDocumentSubtypes.FirstOrDefaultAsync(s => s.IdGeneralDocumentSubtype == dto.IdGeneralDocumentSubtype)
                    ?? throw new KeyNotFoundException("Subtype not found.");
                if (entity.IdGeneralDocumentType != dto.IdGeneralDocumentType &&
                    await _db.GeneralDocuments.AnyAsync(d => d.IdGeneralDocumentSubtype == entity.IdGeneralDocumentSubtype))
                    throw new InvalidOperationException("Documents are registered under this subtype, so its document type cannot be changed.");
                entity.ModifiedBy = idEmployee;
                entity.ModifiedDateTime = CompanyTime.Now;
            }
            else
            {
                entity = new GeneralDocumentSubtypes { CreatedBy = idEmployee, CreatedDateTime = CompanyTime.Now };
                _db.GeneralDocumentSubtypes.Add(entity);
            }
            entity.IdGeneralDocumentType = dto.IdGeneralDocumentType;
            entity.SubtypeCode = code;
            entity.SubtypeName = name;
            entity.Description = Clean(dto.Description);
            entity.DefaultMaintenanceType = maint;
            entity.DefaultScope = scope;
            entity.PeriodFrequency = freq;
            entity.ValidityRequired = dto.ValidityRequired;
            entity.ExpiryAlertDays = dto.ExpiryAlertDays;
            entity.VisibleToAllEmployees = dto.VisibleToAllEmployees;
            // acknowledgement only makes sense for documents employees can see
            entity.AcknowledgementRequired = dto.VisibleToAllEmployees && dto.AcknowledgementRequired;
            entity.DisplayOrder = dto.DisplayOrder;
            entity.IsActive = dto.IsActive;
            await _db.SaveChangesAsync();
            return entity.IdGeneralDocumentSubtype;
        }

        #endregion

        #region Office access

        /// <summary>null = all offices; otherwise the office of the user and its child offices.</summary>
        private async Task<HashSet<int>?> AccessibleOffices(GeneralDocumentUserContext user)
        {
            if (user.AllOfficesPermission) return null;
            var myOffice = await _db.Employees.AsNoTracking().Where(e => e.IdEmployee == user.IdEmployee).Select(e => e.IdOffice).FirstOrDefaultAsync();
            if (myOffice == null) return null;

            var all = await _db.Offices.AsNoTracking().Select(o => new { o.IdOffice, o.IdParentOffice }).ToListAsync();
            var result = new HashSet<int> { myOffice.Value };
            var queue = new Queue<int>(result);
            while (queue.Count > 0)
            {
                var id = queue.Dequeue();
                foreach (var child in all.Where(o => o.IdParentOffice == id && o.IdOffice != id))
                    if (result.Add(child.IdOffice)) queue.Enqueue(child.IdOffice);
            }
            return result;
        }

        private static bool CanSee(GeneralDocuments d, HashSet<int>? offices) =>
            offices == null || d.Scope == "ORGANIZATION" || (d.IdOffice.HasValue && offices.Contains(d.IdOffice.Value));

        // Organisation documents are managed only by users with access to all offices
        private static bool CanManage(GeneralDocuments d, HashSet<int>? offices) =>
            offices == null || (d.Scope == "OFFICE" && d.IdOffice.HasValue && offices.Contains(d.IdOffice.Value));

        private async Task<(GeneralDocuments doc, HashSet<int>? offices)> LoadForManage(int idGeneralDocument, GeneralDocumentUserContext user, bool tracked = true)
        {
            var q = tracked ? _db.GeneralDocuments : _db.GeneralDocuments.AsNoTracking();
            var doc = await q.FirstOrDefaultAsync(d => d.IdGeneralDocument == idGeneralDocument)
                ?? throw new KeyNotFoundException("Document not found.");
            var offices = await AccessibleOffices(user);
            if (!CanSee(doc, offices)) throw new KeyNotFoundException("Document not found.");
            if (!CanManage(doc, offices))
                throw new InvalidOperationException(doc.Scope == "ORGANIZATION"
                    ? "Organisation level documents can be changed only by users with access to all offices."
                    : "You do not have access to this office's documents.");
            return (doc, offices);
        }

        #endregion

        #region Register

        public async Task<GeneralDocumentLookupsDto> GetLookups(GeneralDocumentUserContext user)
        {
            var offices = await AccessibleOffices(user);
            var officeQuery = _db.Offices.AsNoTracking().Where(o => o.IsActive);
            if (offices != null) { var ids = offices.ToList(); officeQuery = officeQuery.Where(o => ids.Contains(o.IdOffice)); }

            return new GeneralDocumentLookupsDto
            {
                Types = await GetTypes(false),
                Subtypes = await GetSubtypes(null, false),
                Offices = await officeQuery.OrderBy(o => o.OfficeName)
                    .Select(o => new GeneralDocumentOfficeDto { IdOffice = o.IdOffice, OfficeCode = o.OfficeCode, OfficeName = o.OfficeName })
                    .ToListAsync(),
                AllOffices = offices == null,
                CanRegister = user.CanRegister,
                CanEdit = user.CanEdit,
                CanUpload = user.CanUpload,
                CanMarkStatus = user.CanMarkStatus,
                MaxFileSizeBytes = MaxFileBytes,
                MaxFilesPerUpload = MaxFiles,
                AllowedExtensions = AllowedExtensions.ToList()
            };
        }

        public async Task<List<GeneralDocumentListDto>> GetDocuments(GeneralDocumentFilterDto filter, GeneralDocumentUserContext user)
        {
            filter ??= new GeneralDocumentFilterDto();
            var rows = await QueryDocuments(filter, user);

            var validity = filter.Validity?.Trim().ToUpperInvariant();
            if (!string.IsNullOrEmpty(validity))
                rows = validity == "ATTENTION"
                    ? rows.Where(r => r.ValidityStatus is "EXPIRED" or "EXPIRING" or "NO_RECORD").ToList()
                    : rows.Where(r => r.ValidityStatus == validity).ToList();

            return rows;
        }

        public async Task<GeneralDocumentSummaryDto> GetSummary(GeneralDocumentFilterDto filter, GeneralDocumentUserContext user)
        {
            filter ??= new GeneralDocumentFilterDto();
            filter.IncludeInactive = false;
            var rows = await QueryDocuments(filter, user);
            return new GeneralDocumentSummaryDto
            {
                Total = rows.Count,
                Valid = rows.Count(r => r.ValidityStatus == "VALID"),
                NoExpiry = rows.Count(r => r.ValidityStatus == "NO_EXPIRY"),
                Expiring = rows.Count(r => r.ValidityStatus == "EXPIRING"),
                Expired = rows.Count(r => r.ValidityStatus == "EXPIRED"),
                NoRecord = rows.Count(r => r.ValidityStatus == "NO_RECORD")
            };
        }

        private async Task<List<GeneralDocumentListDto>> QueryDocuments(GeneralDocumentFilterDto filter, GeneralDocumentUserContext user, List<int>? onlyIds = null)
        {
            var offices = await AccessibleOffices(user);
            var q = _db.GeneralDocuments.AsNoTracking().AsQueryable();

            if (onlyIds != null) q = q.Where(d => onlyIds.Contains(d.IdGeneralDocument));
            if (!filter.IncludeInactive) q = q.Where(d => d.IsActive);
            if (filter.IdGeneralDocumentType is > 0) q = q.Where(d => d.IdGeneralDocumentType == filter.IdGeneralDocumentType);
            if (filter.IdGeneralDocumentSubtype is > 0) q = q.Where(d => d.IdGeneralDocumentSubtype == filter.IdGeneralDocumentSubtype);
            if (!string.IsNullOrWhiteSpace(filter.Scope)) { var s = filter.Scope.Trim().ToUpperInvariant(); q = q.Where(d => d.Scope == s); }
            if (filter.IdOffice is > 0) q = q.Where(d => d.IdOffice == filter.IdOffice);
            if (!string.IsNullOrWhiteSpace(filter.MaintenanceType)) { var m = filter.MaintenanceType.Trim().ToUpperInvariant(); q = q.Where(d => d.MaintenanceType == m); }
            if (offices != null) { var ids = offices.ToList(); q = q.Where(d => d.Scope == "ORGANIZATION" || (d.IdOffice != null && ids.Contains(d.IdOffice.Value))); }

            if (!string.IsNullOrWhiteSpace(filter.Search))
            {
                foreach (var word in filter.Search.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries).Take(5))
                {
                    var w = word;
                    q = q.Where(d =>
                        d.Title.Contains(w) ||
                        (d.DocumentCode != null && d.DocumentCode.Contains(w)) ||
                        (d.Tags != null && d.Tags.Contains(w)) ||
                        (d.Description != null && d.Description.Contains(w)) ||
                        _db.GeneralDocumentRecords.Any(r => r.IdGeneralDocument == d.IdGeneralDocument && r.RecordStatus != Incorrect &&
                            ((r.ReferenceNumber != null && r.ReferenceNumber.Contains(w)) || (r.PeriodLabel != null && r.PeriodLabel.Contains(w)))));
                }
            }

            var data = await q
                .OrderBy(d => d.Title)
                .Take(5000)
                .Select(d => new
                {
                    d,
                    TypeName = _db.GeneralDocumentTypes.Where(t => t.IdGeneralDocumentType == d.IdGeneralDocumentType).Select(t => t.TypeName).FirstOrDefault(),
                    SubtypeName = _db.GeneralDocumentSubtypes.Where(s => s.IdGeneralDocumentSubtype == d.IdGeneralDocumentSubtype).Select(s => s.SubtypeName).FirstOrDefault(),
                    OfficeName = _db.Offices.Where(o => o.IdOffice == d.IdOffice).Select(o => o.OfficeName).FirstOrDefault(),
                    RecordCount = _db.GeneralDocumentRecords.Count(r => r.IdGeneralDocument == d.IdGeneralDocument),
                    Current = _db.GeneralDocumentRecords
                        .Where(r => r.IdGeneralDocument == d.IdGeneralDocument && r.IsCurrent && r.RecordStatus == Active)
                        .Select(r => new
                        {
                            r.IdGeneralDocumentRecord,
                            r.VersionNumber,
                            r.PeriodLabel,
                            r.PeriodTo,
                            r.ReferenceNumber,
                            r.ValidFrom,
                            r.ValidTo,
                            r.UploadedDateTime,
                            Files = _db.GeneralDocumentFiles.Count(f => f.IdGeneralDocumentRecord == r.IdGeneralDocumentRecord)
                        })
                        .FirstOrDefault()
                })
                .ToListAsync();

            return data.Select(x =>
            {
                var alertDays = filter.WithinDays is >= 0 ? filter.WithinDays.Value : x.d.ExpiryAlertDays;
                var (status, days) = Validity(x.Current != null, x.Current?.ValidTo, alertDays);
                return new GeneralDocumentListDto
                {
                    IdGeneralDocument = x.d.IdGeneralDocument,
                    DocumentCode = x.d.DocumentCode,
                    Title = x.d.Title,
                    IdGeneralDocumentType = x.d.IdGeneralDocumentType,
                    TypeName = x.TypeName,
                    IdGeneralDocumentSubtype = x.d.IdGeneralDocumentSubtype,
                    SubtypeName = x.SubtypeName,
                    Scope = x.d.Scope,
                    IdOffice = x.d.IdOffice,
                    OfficeName = x.OfficeName,
                    MaintenanceType = x.d.MaintenanceType,
                    PeriodFrequency = x.d.PeriodFrequency,
                    ValidityRequired = x.d.ValidityRequired,
                    ExpiryAlertDays = x.d.ExpiryAlertDays,
                    Description = x.d.Description,
                    Tags = x.d.Tags,
                    IsActive = x.d.IsActive,
                    RecordCount = x.RecordCount,
                    IdCurrentRecord = x.Current?.IdGeneralDocumentRecord,
                    VersionNumber = x.Current?.VersionNumber,
                    PeriodLabel = x.Current?.PeriodLabel,
                    PeriodTo = x.Current?.PeriodTo,
                    ReferenceNumber = x.Current?.ReferenceNumber,
                    ValidFrom = x.Current?.ValidFrom,
                    ValidTo = x.Current?.ValidTo,
                    LastUploadedDateTime = x.Current?.UploadedDateTime,
                    FileCount = x.Current?.Files ?? 0,
                    ValidityStatus = status,
                    DaysToExpiry = days
                };
            }).ToList();
        }

        private static (string status, int? days) Validity(bool hasRecord, DateTime? validTo, int alertDays)
        {
            if (!hasRecord) return ("NO_RECORD", null);
            if (validTo == null) return ("NO_EXPIRY", null);
            var days = (validTo.Value.Date - DateTime.Today).Days;
            if (days < 0) return ("EXPIRED", days);
            if (days <= alertDays) return ("EXPIRING", days);
            return ("VALID", days);
        }

        public async Task<GeneralDocumentDetailDto> GetDocument(int idGeneralDocument, GeneralDocumentUserContext user)
        {
            var doc = (await QueryDocuments(new GeneralDocumentFilterDto { IncludeInactive = true }, user, new List<int> { idGeneralDocument })).FirstOrDefault()
                ?? throw new KeyNotFoundException("Document not found.");
            var entity = await _db.GeneralDocuments.AsNoTracking().FirstAsync(d => d.IdGeneralDocument == idGeneralDocument);
            var offices = await AccessibleOffices(user);

            var records = await _db.GeneralDocumentRecords.AsNoTracking()
                .Where(r => r.IdGeneralDocument == idGeneralDocument)
                .OrderByDescending(r => r.VersionNumber)
                .ToListAsync();
            var recordIds = records.Select(r => r.IdGeneralDocumentRecord).ToList();
            var files = await _db.GeneralDocumentFiles.AsNoTracking()
                .Where(f => recordIds.Contains(f.IdGeneralDocumentRecord))
                .OrderBy(f => f.IdGeneralDocumentFile)
                .ToListAsync();
            var audit = await _db.GeneralDocumentAudit.AsNoTracking()
                .Where(a => a.IdGeneralDocument == idGeneralDocument)
                .OrderByDescending(a => a.ActionDateTime).ThenByDescending(a => a.IdGeneralDocumentAudit)
                .Take(300)
                .ToListAsync();

            var names = await EmployeeNames(records.Select(r => r.UploadedBy).Concat(records.Select(r => r.StatusChangedBy)).Concat(audit.Select(a => a.ActionBy)));
            var versionOf = records.ToDictionary(r => r.IdGeneralDocumentRecord, r => r.VersionNumber);

            return new GeneralDocumentDetailDto
            {
                Document = doc,
                CanManage = CanManage(entity, offices),
                Records = records.Select(r => new GeneralDocumentRecordDto
                {
                    IdGeneralDocumentRecord = r.IdGeneralDocumentRecord,
                    VersionNumber = r.VersionNumber,
                    PeriodLabel = r.PeriodLabel,
                    PeriodFrom = r.PeriodFrom,
                    PeriodTo = r.PeriodTo,
                    ReferenceNumber = r.ReferenceNumber,
                    IssuingAuthority = r.IssuingAuthority,
                    IssueDate = r.IssueDate,
                    ValidFrom = r.ValidFrom,
                    ValidTo = r.ValidTo,
                    Remarks = r.Remarks,
                    RecordStatus = r.RecordStatus,
                    IsCurrent = r.IsCurrent,
                    StatusRemarks = r.StatusRemarks,
                    StatusChangedByName = Name(names, r.StatusChangedBy),
                    StatusChangedDateTime = r.StatusChangedDateTime,
                    SupersededByVersion = r.SupersededByIdRecord.HasValue && versionOf.TryGetValue(r.SupersededByIdRecord.Value, out var v) ? v : null,
                    UploadedByName = Name(names, r.UploadedBy),
                    UploadedDateTime = r.UploadedDateTime,
                    ValidityStatus = r.RecordStatus == Active ? Validity(true, r.ValidTo, entity.ExpiryAlertDays).status : null,
                    Files = files.Where(f => f.IdGeneralDocumentRecord == r.IdGeneralDocumentRecord).Select(f => new GeneralDocumentFileDto
                    {
                        IdGeneralDocumentFile = f.IdGeneralDocumentFile,
                        OriginalFileName = f.OriginalFileName,
                        MimeType = f.MimeType,
                        FileSize = f.FileSize,
                        UploadedDateTime = f.UploadedDateTime
                    }).ToList()
                }).ToList(),
                Audit = audit.Select(a => new GeneralDocumentAuditDto
                {
                    Action = a.Action,
                    VersionNumber = a.IdGeneralDocumentRecord.HasValue && versionOf.TryGetValue(a.IdGeneralDocumentRecord.Value, out var av) ? av : null,
                    Details = a.Details,
                    ActionByName = Name(names, a.ActionBy),
                    ActionDateTime = a.ActionDateTime
                }).ToList()
            };
        }

        public async Task<int> SaveDocument(SaveGeneralDocumentDto dto, GeneralDocumentUserContext user)
        {
            if (dto == null) throw new InvalidOperationException("Enter the document details.");
            var isNew = dto.IdGeneralDocument <= 0;
            if (isNew && !user.CanRegister) throw new UnauthorizedAccessException();
            if (!isNew && !user.CanEdit) throw new UnauthorizedAccessException();

            var title = (dto.Title ?? "").Trim();
            var scope = (dto.Scope ?? "").Trim().ToUpperInvariant();
            var maint = (dto.MaintenanceType ?? "").Trim().ToUpperInvariant();
            var freq = string.IsNullOrWhiteSpace(dto.PeriodFrequency) ? null : dto.PeriodFrequency.Trim().ToUpperInvariant();
            int? idOffice = scope == "OFFICE" ? dto.IdOffice : null;

            if (title.Length == 0) throw new InvalidOperationException("Enter the document title.");
            if (title.Length > 200) throw new InvalidOperationException("Title must not exceed 200 characters.");
            if (!Scopes.Contains(scope)) throw new InvalidOperationException("Select the scope (Organisation or Office).");
            if (scope == "OFFICE" && !(idOffice > 0)) throw new InvalidOperationException("Select the office.");
            if (!MaintenanceTypes.Contains(maint)) throw new InvalidOperationException("Select the maintenance type.");
            if (maint != "PERIODIC") freq = null;
            else if (freq == null) throw new InvalidOperationException("Select the period frequency.");
            else if (!Frequencies.Contains(freq)) throw new InvalidOperationException("Select a valid period frequency.");
            if (dto.ExpiryAlertDays < 0 || dto.ExpiryAlertDays > 365) throw new InvalidOperationException("Expiry alert days must be between 0 and 365.");
            if (dto.Description?.Trim().Length > 500) throw new InvalidOperationException("Description must not exceed 500 characters.");
            var tags = NormaliseTags(dto.Tags);
            if (tags?.Length > 250) throw new InvalidOperationException("Tags must not exceed 250 characters.");

            var type = await _db.GeneralDocumentTypes.AsNoTracking().FirstOrDefaultAsync(t => t.IdGeneralDocumentType == dto.IdGeneralDocumentType)
                ?? throw new InvalidOperationException("Select the document type.");
            GeneralDocumentSubtypes? subtype = null;
            if (dto.IdGeneralDocumentSubtype is > 0)
            {
                subtype = await _db.GeneralDocumentSubtypes.AsNoTracking().FirstOrDefaultAsync(s => s.IdGeneralDocumentSubtype == dto.IdGeneralDocumentSubtype);
                if (subtype == null || subtype.IdGeneralDocumentType != type.IdGeneralDocumentType)
                    throw new InvalidOperationException("Select a subtype of the selected document type.");
            }

            var offices = await AccessibleOffices(user);
            if (scope == "ORGANIZATION" && offices != null)
                throw new InvalidOperationException("Organisation level documents can be registered only by users with access to all offices. Select an office.");
            if (scope == "OFFICE")
            {
                if (!await _db.Offices.AnyAsync(o => o.IdOffice == idOffice)) throw new InvalidOperationException("Select a valid office.");
                if (offices != null && !offices.Contains(idOffice!.Value)) throw new InvalidOperationException("You do not have access to the selected office.");
            }

            if (await _db.GeneralDocuments.AnyAsync(d => d.IdGeneralDocument != dto.IdGeneralDocument && d.Scope == scope && d.IdOffice == idOffice && d.Title == title))
                throw new InvalidOperationException(scope == "OFFICE"
                    ? $"A document titled \"{title}\" is already registered for this office."
                    : $"A document titled \"{title}\" is already registered at organisation level.");

            GeneralDocuments doc;
            Dictionary<string, object?>? changes = null;
            if (isNew)
            {
                if (!type.IsActive) throw new InvalidOperationException("The selected document type is inactive.");
                if (subtype != null && !subtype.IsActive) throw new InvalidOperationException("The selected subtype is inactive.");
                doc = new GeneralDocuments { IsActive = true, CreatedBy = user.IdEmployee, CreatedDateTime = CompanyTime.Now };
                _db.GeneralDocuments.Add(doc);
            }
            else
            {
                (doc, _) = await LoadForManage(dto.IdGeneralDocument, user);
                if (doc.MaintenanceType != maint && await _db.GeneralDocumentRecords.AnyAsync(r => r.IdGeneralDocument == doc.IdGeneralDocument))
                    throw new InvalidOperationException("The maintenance type cannot be changed after files are uploaded.");
                changes = new Dictionary<string, object?>();
                void Track(string field, object? oldValue, object? newValue)
                {
                    if (!Equals(oldValue, newValue)) changes[field] = new { From = oldValue, To = newValue };
                }
                Track("Title", doc.Title, title);
                Track("IdGeneralDocumentType", doc.IdGeneralDocumentType, type.IdGeneralDocumentType);
                Track("IdGeneralDocumentSubtype", doc.IdGeneralDocumentSubtype, subtype?.IdGeneralDocumentSubtype);
                Track("Scope", doc.Scope, scope);
                Track("IdOffice", doc.IdOffice, idOffice);
                Track("MaintenanceType", doc.MaintenanceType, maint);
                Track("PeriodFrequency", doc.PeriodFrequency, freq);
                Track("ValidityRequired", doc.ValidityRequired, dto.ValidityRequired);
                Track("ExpiryAlertDays", doc.ExpiryAlertDays, dto.ExpiryAlertDays);
                Track("Description", doc.Description, Clean(dto.Description));
                Track("Tags", doc.Tags, tags);
                if (changes.Count == 0) return doc.IdGeneralDocument;
                doc.ModifiedBy = user.IdEmployee;
                doc.ModifiedDateTime = CompanyTime.Now;
            }

            doc.Title = title;
            doc.IdGeneralDocumentType = type.IdGeneralDocumentType;
            doc.IdGeneralDocumentSubtype = subtype?.IdGeneralDocumentSubtype;
            doc.Scope = scope;
            doc.IdOffice = idOffice;
            doc.MaintenanceType = maint;
            doc.PeriodFrequency = freq;
            doc.ValidityRequired = dto.ValidityRequired;
            doc.ExpiryAlertDays = dto.ExpiryAlertDays;
            doc.Description = Clean(dto.Description);
            doc.Tags = tags;

            await using var tx = await _db.Database.BeginTransactionAsync();
            await _db.SaveChangesAsync();
            if (isNew)
            {
                doc.DocumentCode = $"GD-{doc.IdGeneralDocument:000000}";
                AddAudit(doc.IdGeneralDocument, null, null, "CREATE", new { doc.DocumentCode, doc.Title, Type = type.TypeName, Subtype = subtype?.SubtypeName, doc.Scope, doc.IdOffice, doc.MaintenanceType, doc.PeriodFrequency }, user);
            }
            else
            {
                AddAudit(doc.IdGeneralDocument, null, null, "UPDATE", changes, user);
            }
            await _db.SaveChangesAsync();
            await tx.CommitAsync();
            return doc.IdGeneralDocument;
        }

        public async Task SetDocumentActive(int idGeneralDocument, bool isActive, string? remarks, GeneralDocumentUserContext user)
        {
            var (doc, _) = await LoadForManage(idGeneralDocument, user);
            if (doc.IsActive == isActive) return;
            if (!isActive && string.IsNullOrWhiteSpace(remarks)) throw new InvalidOperationException("Enter the reason for deactivating the document.");
            if (remarks?.Trim().Length > 500) throw new InvalidOperationException("Remarks must not exceed 500 characters.");
            doc.IsActive = isActive;
            doc.ModifiedBy = user.IdEmployee;
            doc.ModifiedDateTime = CompanyTime.Now;
            AddAudit(doc.IdGeneralDocument, null, null, isActive ? "ACTIVATE" : "DEACTIVATE", new { Remarks = Clean(remarks) }, user);
            await _db.SaveChangesAsync();
        }

        #endregion

        #region Records

        public async Task<int> UploadRecord(UploadGeneralDocumentRecordDto dto, GeneralDocumentUserContext user)
        {
            if (dto == null) throw new InvalidOperationException("Enter the upload details.");
            var (doc, _) = await LoadForManage(dto.IdGeneralDocument, user);
            if (!doc.IsActive) throw new InvalidOperationException("This document is inactive. Activate it before uploading.");

            var files = (dto.Files ?? new List<IFormFile>()).Where(f => f != null).ToList();
            ValidateFiles(files);
            var values = new RecordValues(dto.PeriodLabel, dto.PeriodFrom, dto.PeriodTo, dto.ReferenceNumber, dto.IssuingAuthority, dto.IssueDate, dto.ValidFrom, dto.ValidTo, dto.Remarks);
            ValidateRecord(values, doc);

            var existing = await _db.GeneralDocumentRecords.Where(r => r.IdGeneralDocument == doc.IdGeneralDocument).ToListAsync();
            var active = existing.Where(r => r.RecordStatus == Active).ToList();
            List<GeneralDocumentRecords> toSupersede;
            if (doc.MaintenanceType == "PERIODIC")
            {
                var label = values.PeriodLabel!;
                toSupersede = active.Where(r =>
                    (r.PeriodFrom?.Date == values.PeriodFrom?.Date && r.PeriodTo?.Date == values.PeriodTo?.Date) ||
                    string.Equals(r.PeriodLabel?.Trim(), label, StringComparison.OrdinalIgnoreCase)).ToList();
                if (toSupersede.Count > 0 && string.IsNullOrWhiteSpace(values.Remarks))
                    throw new InvalidOperationException($"A document is already uploaded for {label}. Enter remarks explaining why it is being replaced.");
                var overlap = active.Except(toSupersede).FirstOrDefault(r => r.PeriodFrom.HasValue && r.PeriodTo.HasValue &&
                    r.PeriodFrom.Value.Date <= values.PeriodTo!.Value.Date && r.PeriodTo.Value.Date >= values.PeriodFrom!.Value.Date);
                if (overlap != null)
                    throw new InvalidOperationException($"The period overlaps {overlap.PeriodLabel} ({overlap.PeriodFrom:dd-MMM-yyyy} to {overlap.PeriodTo:dd-MMM-yyyy}). Mark that record superseded first if it is wrong.");
            }
            else
            {
                toSupersede = active;
                if (doc.MaintenanceType == "FIXED" && active.Count > 0 && string.IsNullOrWhiteSpace(values.Remarks))
                    throw new InvalidOperationException("This document already has a valid file. Enter remarks explaining why it is being replaced.");
            }

            var root = StorageRoot;
            var written = new List<string>();
            await using var tx = await _db.Database.BeginTransactionAsync();
            try
            {
                var record = new GeneralDocumentRecords
                {
                    IdGeneralDocument = doc.IdGeneralDocument,
                    VersionNumber = (existing.Count == 0 ? 0 : existing.Max(r => r.VersionNumber)) + 1,
                    RecordStatus = Active,
                    UploadedBy = user.IdEmployee,
                    UploadedDateTime = CompanyTime.Now
                };
                values.ApplyTo(record);
                _db.GeneralDocumentRecords.Add(record);
                await _db.SaveChangesAsync();

                var saved = new List<object>();
                foreach (var file in files)
                {
                    var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
                    var relative = $"{CompanyTime.Now:yyyy}/{CompanyTime.Now:MM}/{Guid.NewGuid():N}{ext}";
                    var full = Path.Combine(root, relative);
                    Directory.CreateDirectory(Path.GetDirectoryName(full)!);

                    string hash;
                    await using (var target = new FileStream(full, FileMode.CreateNew, FileAccess.Write))
                    {
                        written.Add(full);
                        await file.CopyToAsync(target);
                    }
                    await using (var read = File.OpenRead(full)) hash = Convert.ToHexString(await SHA256.HashDataAsync(read)).ToLowerInvariant();

                    var entity = new GeneralDocumentFiles
                    {
                        IdGeneralDocumentRecord = record.IdGeneralDocumentRecord,
                        OriginalFileName = SafeFileName(file.FileName),
                        StoredFileName = relative,
                        MimeType = MimeOf(ext),
                        FileSize = file.Length,
                        FileHash = hash,
                        UploadedBy = user.IdEmployee,
                        UploadedDateTime = CompanyTime.Now
                    };
                    _db.GeneralDocumentFiles.Add(entity);
                    saved.Add(new { entity.OriginalFileName, entity.FileSize, entity.FileHash });
                }

                foreach (var old in toSupersede)
                {
                    old.RecordStatus = Superseded;
                    old.IsCurrent = false;
                    old.SupersededByIdRecord = record.IdGeneralDocumentRecord;
                    old.StatusRemarks = $"Replaced by version {record.VersionNumber}" + (string.IsNullOrWhiteSpace(values.Remarks) ? "" : $": {values.Remarks}");
                    old.StatusChangedBy = user.IdEmployee;
                    old.StatusChangedDateTime = CompanyTime.Now;
                }

                AddAudit(doc.IdGeneralDocument, record.IdGeneralDocumentRecord, null, "UPLOAD", new
                {
                    record.VersionNumber,
                    record.PeriodLabel,
                    record.ReferenceNumber,
                    record.ValidFrom,
                    record.ValidTo,
                    Files = saved,
                    Superseded = toSupersede.Select(r => r.VersionNumber).ToList()
                }, user);
                await _db.SaveChangesAsync();
                await RecomputeCurrent(doc);
                await _db.SaveChangesAsync();
                await tx.CommitAsync();
                return record.IdGeneralDocumentRecord;
            }
            catch
            {
                foreach (var path in written)
                {
                    try { File.Delete(path); } catch (Exception ex) { _logger.LogWarning(ex, "Could not remove {Path} after a failed upload", path); }
                }
                throw;
            }
        }

        public async Task UpdateRecord(UpdateGeneralDocumentRecordDto dto, GeneralDocumentUserContext user)
        {
            if (dto == null) throw new InvalidOperationException("Enter the record details.");
            var record = await _db.GeneralDocumentRecords.FirstOrDefaultAsync(r => r.IdGeneralDocumentRecord == dto.IdGeneralDocumentRecord)
                ?? throw new KeyNotFoundException("Record not found.");
            var (doc, _) = await LoadForManage(record.IdGeneralDocument, user);
            if (record.RecordStatus != Active) throw new InvalidOperationException("Only active records can be corrected.");

            var values = new RecordValues(dto.PeriodLabel, dto.PeriodFrom, dto.PeriodTo, dto.ReferenceNumber, dto.IssuingAuthority, dto.IssueDate, dto.ValidFrom, dto.ValidTo, dto.Remarks);
            ValidateRecord(values, doc);

            if (doc.MaintenanceType == "PERIODIC")
            {
                var others = await _db.GeneralDocumentRecords.AsNoTracking()
                    .Where(r => r.IdGeneralDocument == doc.IdGeneralDocument && r.RecordStatus == Active && r.IdGeneralDocumentRecord != record.IdGeneralDocumentRecord)
                    .ToListAsync();
                var clash = others.FirstOrDefault(r =>
                    string.Equals(r.PeriodLabel?.Trim(), values.PeriodLabel, StringComparison.OrdinalIgnoreCase) ||
                    (r.PeriodFrom.HasValue && r.PeriodTo.HasValue && r.PeriodFrom.Value.Date <= values.PeriodTo!.Value.Date && r.PeriodTo.Value.Date >= values.PeriodFrom!.Value.Date));
                if (clash != null) throw new InvalidOperationException($"Another active record already covers this period ({clash.PeriodLabel}).");
            }

            var before = new RecordValues(record.PeriodLabel, record.PeriodFrom, record.PeriodTo, record.ReferenceNumber, record.IssuingAuthority, record.IssueDate, record.ValidFrom, record.ValidTo, record.Remarks);
            var changes = before.Diff(values);
            if (changes.Count == 0) return;
            values.ApplyTo(record);
            AddAudit(doc.IdGeneralDocument, record.IdGeneralDocumentRecord, null, "RECORD_UPDATE", new { record.VersionNumber, Changes = changes }, user);

            await using var tx = await _db.Database.BeginTransactionAsync();
            await _db.SaveChangesAsync();
            await RecomputeCurrent(doc);
            await _db.SaveChangesAsync();
            await tx.CommitAsync();
        }

        public async Task SetRecordStatus(GeneralDocumentRecordStatusDto dto, GeneralDocumentUserContext user)
        {
            if (dto == null) throw new InvalidOperationException("Select the record.");
            var status = (dto.Status ?? "").Trim().ToUpperInvariant();
            if (status != Incorrect && status != Superseded) throw new InvalidOperationException("Select Incorrect or Superseded.");
            var remarks = Clean(dto.Remarks);
            if (remarks == null) throw new InvalidOperationException("Enter the remarks.");
            if (remarks.Length > 500) throw new InvalidOperationException("Remarks must not exceed 500 characters.");

            var record = await _db.GeneralDocumentRecords.FirstOrDefaultAsync(r => r.IdGeneralDocumentRecord == dto.IdGeneralDocumentRecord)
                ?? throw new KeyNotFoundException("Record not found.");
            var (doc, _) = await LoadForManage(record.IdGeneralDocument, user);

            if (record.RecordStatus == status) throw new InvalidOperationException($"This record is already marked {status.ToLowerInvariant()}.");
            if (record.RecordStatus == Incorrect) throw new InvalidOperationException("A record marked incorrect cannot be changed.");
            if (status == Superseded && record.RecordStatus != Active) throw new InvalidOperationException("Only an active record can be marked superseded.");

            var restored = new List<int>();
            record.RecordStatus = status;
            record.IsCurrent = false;
            record.StatusRemarks = remarks;
            record.StatusChangedBy = user.IdEmployee;
            record.StatusChangedDateTime = CompanyTime.Now;

            if (status == Incorrect)
            {
                // A wrong upload should not leave the document without its earlier valid file:
                // records that this one replaced become active again.
                var replaced = await _db.GeneralDocumentRecords
                    .Where(r => r.IdGeneralDocument == doc.IdGeneralDocument && r.SupersededByIdRecord == record.IdGeneralDocumentRecord && r.RecordStatus == Superseded)
                    .ToListAsync();
                foreach (var r in replaced)
                {
                    r.RecordStatus = Active;
                    r.SupersededByIdRecord = null;
                    r.StatusRemarks = $"Restored because version {record.VersionNumber} was marked incorrect.";
                    r.StatusChangedBy = user.IdEmployee;
                    r.StatusChangedDateTime = CompanyTime.Now;
                    restored.Add(r.VersionNumber);
                }
            }

            AddAudit(doc.IdGeneralDocument, record.IdGeneralDocumentRecord, null, status == Incorrect ? "MARK_INCORRECT" : "MARK_SUPERSEDED",
                new { record.VersionNumber, Remarks = remarks, Restored = restored }, user);

            await using var tx = await _db.Database.BeginTransactionAsync();
            await _db.SaveChangesAsync();
            await RecomputeCurrent(doc);
            await _db.SaveChangesAsync();
            await tx.CommitAsync();
        }

        /// <summary>Exactly one active record is current: the latest version, or for periodic documents the latest period.</summary>
        private async Task RecomputeCurrent(GeneralDocuments doc)
        {
            var records = await _db.GeneralDocumentRecords.Where(r => r.IdGeneralDocument == doc.IdGeneralDocument).ToListAsync();
            var active = records.Where(r => r.RecordStatus == Active);
            var current = doc.MaintenanceType == "PERIODIC"
                ? active.OrderByDescending(r => r.PeriodTo ?? DateTime.MinValue).ThenByDescending(r => r.PeriodFrom ?? DateTime.MinValue).ThenByDescending(r => r.VersionNumber).FirstOrDefault()
                : active.OrderByDescending(r => r.VersionNumber).FirstOrDefault();
            foreach (var r in records)
            {
                var shouldBe = current != null && r.IdGeneralDocumentRecord == current.IdGeneralDocumentRecord;
                if (r.IsCurrent != shouldBe) r.IsCurrent = shouldBe;
            }
        }

        private sealed record RecordValues(string? PeriodLabel, DateTime? PeriodFrom, DateTime? PeriodTo, string? ReferenceNumber, string? IssuingAuthority,
            DateTime? IssueDate, DateTime? ValidFrom, DateTime? ValidTo, string? Remarks)
        {
            public void ApplyTo(GeneralDocumentRecords r)
            {
                r.PeriodLabel = Clean(PeriodLabel);
                r.PeriodFrom = PeriodFrom?.Date;
                r.PeriodTo = PeriodTo?.Date;
                r.ReferenceNumber = Clean(ReferenceNumber);
                r.IssuingAuthority = Clean(IssuingAuthority);
                r.IssueDate = IssueDate?.Date;
                r.ValidFrom = ValidFrom?.Date;
                r.ValidTo = ValidTo?.Date;
                r.Remarks = Clean(Remarks);
            }

            public Dictionary<string, object?> Diff(RecordValues n)
            {
                var d = new Dictionary<string, object?>();
                void T(string f, object? a, object? b) { if (!Equals(a, b)) d[f] = new { From = a, To = b }; }
                T("PeriodLabel", Clean(PeriodLabel), Clean(n.PeriodLabel));
                T("PeriodFrom", PeriodFrom?.Date, n.PeriodFrom?.Date);
                T("PeriodTo", PeriodTo?.Date, n.PeriodTo?.Date);
                T("ReferenceNumber", Clean(ReferenceNumber), Clean(n.ReferenceNumber));
                T("IssuingAuthority", Clean(IssuingAuthority), Clean(n.IssuingAuthority));
                T("IssueDate", IssueDate?.Date, n.IssueDate?.Date);
                T("ValidFrom", ValidFrom?.Date, n.ValidFrom?.Date);
                T("ValidTo", ValidTo?.Date, n.ValidTo?.Date);
                T("Remarks", Clean(Remarks), Clean(n.Remarks));
                return d;
            }
        }

        private static void ValidateRecord(RecordValues v, GeneralDocuments doc)
        {
            if (v.PeriodLabel?.Trim().Length > 50) throw new InvalidOperationException("Period must not exceed 50 characters.");
            if (v.ReferenceNumber?.Trim().Length > 100) throw new InvalidOperationException("Reference number must not exceed 100 characters.");
            if (v.IssuingAuthority?.Trim().Length > 150) throw new InvalidOperationException("Issuing authority must not exceed 150 characters.");
            if (v.Remarks?.Trim().Length > 500) throw new InvalidOperationException("Remarks must not exceed 500 characters.");
            if (v.IssueDate?.Date > DateTime.Today) throw new InvalidOperationException("Issue date cannot be in the future.");
            if (v.ValidFrom.HasValue && v.ValidTo.HasValue && v.ValidTo.Value.Date < v.ValidFrom.Value.Date)
                throw new InvalidOperationException("Valid To cannot be before Valid From.");
            if (doc.ValidityRequired && !v.ValidTo.HasValue) throw new InvalidOperationException("Enter the Valid To date for this document.");

            if (doc.MaintenanceType == "PERIODIC")
            {
                if (string.IsNullOrWhiteSpace(v.PeriodLabel)) throw new InvalidOperationException("Enter the period (e.g. FY 2025-26 or Oct 2026).");
                if (!v.PeriodFrom.HasValue || !v.PeriodTo.HasValue) throw new InvalidOperationException("Enter the period from and to dates.");
                if (v.PeriodTo.Value.Date < v.PeriodFrom.Value.Date) throw new InvalidOperationException("Period To cannot be before Period From.");
            }
            else if (!string.IsNullOrWhiteSpace(v.PeriodLabel) || v.PeriodFrom.HasValue || v.PeriodTo.HasValue)
            {
                throw new InvalidOperationException("Period applies only to periodic documents.");
            }
        }

        private void ValidateFiles(List<IFormFile> files)
        {
            if (files.Count == 0) throw new InvalidOperationException("Select at least one file to upload.");
            if (files.Count > MaxFiles) throw new InvalidOperationException($"You can upload up to {MaxFiles} files at a time.");
            var allowed = AllowedExtensions;
            foreach (var f in files)
            {
                var ext = Path.GetExtension(f.FileName ?? "").ToLowerInvariant();
                if (f.Length == 0) throw new InvalidOperationException($"{f.FileName} is empty.");
                if (f.Length > MaxFileBytes) throw new InvalidOperationException($"{f.FileName} is larger than {MaxFileBytes / 1024d / 1024d:0.#} MB.");
                if (!allowed.Contains(ext)) throw new InvalidOperationException($"{f.FileName}: only {string.Join(", ", allowed)} files are allowed.");
                if ((f.FileName ?? "").Length > 255) throw new InvalidOperationException("File name must not exceed 255 characters.");
            }
        }

        #endregion

        #region Files

        public async Task<FileDownloadDto> GetFile(int idGeneralDocumentFile, bool inline, GeneralDocumentUserContext user)
        {
            var row = await (
                from f in _db.GeneralDocumentFiles.AsNoTracking()
                join r in _db.GeneralDocumentRecords.AsNoTracking() on f.IdGeneralDocumentRecord equals r.IdGeneralDocumentRecord
                join d in _db.GeneralDocuments.AsNoTracking() on r.IdGeneralDocument equals d.IdGeneralDocument
                where f.IdGeneralDocumentFile == idGeneralDocumentFile
                select new { f, r.IdGeneralDocumentRecord, d }).FirstOrDefaultAsync()
                ?? throw new KeyNotFoundException("File not found.");
            var offices = await AccessibleOffices(user);
            if (!CanSee(row.d, offices)) throw new KeyNotFoundException("File not found.");

            var path = FullPath(row.f.StoredFileName);
            if (path == null || !File.Exists(path))
            {
                _logger.LogWarning("General document file {Id} missing at {Path}", idGeneralDocumentFile, row.f.StoredFileName);
                throw new KeyNotFoundException("The file was not found on the server.");
            }

            AddAudit(row.d.IdGeneralDocument, row.IdGeneralDocumentRecord, row.f.IdGeneralDocumentFile, inline ? "VIEW" : "DOWNLOAD", new { row.f.OriginalFileName }, user);
            await _db.SaveChangesAsync();

            return new FileDownloadDto
            {
                Content = await File.ReadAllBytesAsync(path),
                ContentType = row.f.MimeType ?? MimeOf(Path.GetExtension(row.f.OriginalFileName)),
                FileName = row.f.OriginalFileName
            };
        }

        public async Task<FileDownloadDto> GetZip(GeneralDocumentZipRequestDto request, GeneralDocumentUserContext user)
        {
            var docIds = (request?.DocumentIds ?? new List<int>()).Distinct().ToList();
            var recIds = (request?.RecordIds ?? new List<int>()).Distinct().ToList();
            if (docIds.Count == 0 && recIds.Count == 0) throw new InvalidOperationException("Select the documents to download.");
            if (docIds.Count + recIds.Count > 500) throw new InvalidOperationException("Select up to 500 documents at a time.");

            var rows = await (
                from f in _db.GeneralDocumentFiles.AsNoTracking()
                join r in _db.GeneralDocumentRecords.AsNoTracking() on f.IdGeneralDocumentRecord equals r.IdGeneralDocumentRecord
                join d in _db.GeneralDocuments.AsNoTracking() on r.IdGeneralDocument equals d.IdGeneralDocument
                where (docIds.Contains(d.IdGeneralDocument) && r.IsCurrent && r.RecordStatus == Active) || recIds.Contains(r.IdGeneralDocumentRecord)
                orderby d.Title, r.VersionNumber, f.IdGeneralDocumentFile
                select new { f, r, d }).ToListAsync();

            var offices = await AccessibleOffices(user);
            rows = rows.Where(x => CanSee(x.d, offices)).ToList();
            if (rows.Count == 0) throw new InvalidOperationException("The selected documents have no files to download.");
            if (rows.Sum(x => x.f.FileSize) > MaxZipBytes)
                throw new InvalidOperationException($"The selected files are larger than {MaxZipBytes / 1024 / 1024} MB. Select fewer documents.");

            var missing = new List<string>();
            using var ms = new MemoryStream();
            using (var zip = new ZipArchive(ms, ZipArchiveMode.Create, true))
            {
                var used = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (var x in rows)
                {
                    var folder = SafePart($"{x.d.DocumentCode} - {x.d.Title}", 80);
                    var sub = recIds.Count > 0 || x.d.MaintenanceType == "PERIODIC"
                        ? "/" + SafePart($"v{x.r.VersionNumber}{(string.IsNullOrWhiteSpace(x.r.PeriodLabel) ? "" : " " + x.r.PeriodLabel)}{(x.r.RecordStatus == Active ? "" : " (" + x.r.RecordStatus.ToLowerInvariant() + ")")}", 60)
                        : "";
                    var name = $"{folder}{sub}/{SafePart(x.f.OriginalFileName, 120)}";
                    var baseName = name;
                    for (var i = 2; !used.Add(name); i++)
                        name = Path.ChangeExtension(baseName, null) + $" ({i})" + Path.GetExtension(baseName);

                    var path = FullPath(x.f.StoredFileName);
                    if (path == null || !File.Exists(path)) { missing.Add(name); continue; }
                    var entry = zip.CreateEntry(name, CompressionLevel.Fastest);
                    await using var es = entry.Open();
                    await using var fs = File.OpenRead(path);
                    await fs.CopyToAsync(es);
                }
                if (missing.Count > 0)
                {
                    var note = zip.CreateEntry("MISSING FILES.txt");
                    await using var w = new StreamWriter(note.Open());
                    await w.WriteLineAsync("These files are registered but were not found on the server:");
                    foreach (var m in missing) await w.WriteLineAsync(m);
                }
            }

            foreach (var g in rows.GroupBy(x => x.d.IdGeneralDocument))
                AddAudit(g.Key, null, null, "ZIP_DOWNLOAD", new { Records = g.Select(x => x.r.VersionNumber).Distinct().ToList(), Files = g.Count() }, user);
            await _db.SaveChangesAsync();

            return new FileDownloadDto
            {
                Content = ms.ToArray(),
                ContentType = "application/zip",
                FileName = $"Documents_{CompanyTime.Now:yyyyMMdd_HHmm}.zip"
            };
        }

        private string? FullPath(string stored)
        {
            if (string.IsNullOrWhiteSpace(stored)) return null;
            var root = StorageRoot;
            var full = Path.GetFullPath(Path.Combine(root, stored.Replace('/', Path.DirectorySeparatorChar)));
            return full.StartsWith(root, StringComparison.OrdinalIgnoreCase) ? full : null;
        }

        #endregion

        #region Helpers

        private void AddAudit(int? idDoc, int? idRecord, int? idFile, string action, object? details, GeneralDocumentUserContext user)
        {
            _db.GeneralDocumentAudit.Add(new GeneralDocumentAudit
            {
                IdGeneralDocument = idDoc,
                IdGeneralDocumentRecord = idRecord,
                IdGeneralDocumentFile = idFile,
                Action = action,
                Details = details == null ? null : JsonSerializer.Serialize(details),
                ActionBy = user.IdEmployee,
                ActionDateTime = CompanyTime.Now,
                IPAddress = user.IPAddress?.Length > 50 ? user.IPAddress[..50] : user.IPAddress
            });
        }

        private async Task<Dictionary<int, string>> EmployeeNames(IEnumerable<int?> ids)
        {
            var list = ids.Where(i => i.HasValue).Select(i => i!.Value).Distinct().ToList();
            if (list.Count == 0) return new Dictionary<int, string>();
            var rows = await _db.Employees.AsNoTracking()
                .Where(e => e.IdEmployee != null && list.Contains(e.IdEmployee.Value))
                .Select(e => new { Id = e.IdEmployee!.Value, e.EmployeeCode, e.FirstName, e.MiddleName, e.LastName })
                .ToListAsync();
            return rows.ToDictionary(r => r.Id, r =>
                string.Join(" ", new[] { r.FirstName, r.MiddleName, r.LastName }.Where(n => !string.IsNullOrWhiteSpace(n))) +
                (string.IsNullOrWhiteSpace(r.EmployeeCode) ? "" : $" ({r.EmployeeCode})"));
        }

        private static string? Name(Dictionary<int, string> names, int? id) =>
            id.HasValue && names.TryGetValue(id.Value, out var n) ? n : null;

        private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

        private static string? NormaliseTags(string? tags)
        {
            if (string.IsNullOrWhiteSpace(tags)) return null;
            var parts = tags.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Distinct(StringComparer.OrdinalIgnoreCase);
            var joined = string.Join(", ", parts);
            return joined.Length == 0 ? null : joined;
        }

        private static string SafeFileName(string name)
        {
            var n = Path.GetFileName(name.Replace('\\', '/').Split('/').Last());
            foreach (var c in Path.GetInvalidFileNameChars()) n = n.Replace(c, '_');
            return n.Length > 255 ? n[^255..] : n;
        }

        private static string SafePart(string value, int max)
        {
            var s = value;
            foreach (var c in Path.GetInvalidFileNameChars().Concat(new[] { '/', '\\', ':' })) s = s.Replace(c, '_');
            s = s.Trim().TrimEnd('.');
            return s.Length > max ? s[..max].Trim() : s;
        }

        private static string MimeOf(string ext) => ext.ToLowerInvariant() switch
        {
            ".pdf" => "application/pdf",
            ".png" => "image/png",
            ".jpg" or ".jpeg" => "image/jpeg",
            ".gif" => "image/gif",
            ".doc" => "application/msword",
            ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            ".xls" => "application/vnd.ms-excel",
            ".xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            ".txt" => "text/plain",
            ".zip" => "application/zip",
            _ => "application/octet-stream"
        };

        #endregion
    }
}
