using System.Globalization;
using System.Reflection;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Nomaro.API.Database;
using Nomaro.API.DTO;
using Nomaro.API.Helpers;
using Nomaro.API.Models;
using Nomaro.API.Services.Interface;
using R = Nomaro.API.Helpers.ProfileChangeRegistry;

namespace Nomaro.API.Services.Implimentation
{
    /// <summary>
    /// Employee self-service profile change requests.
    /// Requested values are kept in ProfileChangeRequestDetails and written to the master
    /// tables only after final approval (existing workflow engine), inside one transaction.
    /// </summary>
    public class ProfileChangeRequestService : IProfileChangeRequestService
    {
        private readonly ApplicationDBContext _db;
        private readonly IApprovalWorkflowService _workflow;
        private readonly IAuditService _audit;
        private readonly IConfiguration _configuration;
        private readonly IWebHostEnvironment _env;
        private readonly ILogger<ProfileChangeRequestService> _logger;
        private readonly IDataProtector _protector;
        private readonly IEmployeeFamilyService _family;

        private static readonly string[] AllowedExtensions = { ".pdf", ".jpg", ".jpeg", ".png" };
        private const string WorkflowStarted = "Approval workflow initiated.";
        private const string WorkflowApproved = "Record approved and moved to the next level.";
        private const string WorkflowRejected = "Record rejected successfully. Workflow terminated.";

        private Dictionary<int, string>? _banks;
        private Dictionary<int, string>? _branches;
        private Dictionary<int, string>? _docTypes;

        public ProfileChangeRequestService(
            ApplicationDBContext db,
            IApprovalWorkflowService workflow,
            IAuditService audit,
            IConfiguration configuration,
            IWebHostEnvironment env,
            ILogger<ProfileChangeRequestService> logger,
            IDataProtectionProvider dataProtection,
            IEmployeeFamilyService family)
        {
            _family = family;
            _db = db;
            _workflow = workflow;
            _audit = audit;
            _configuration = configuration;
            _env = env;
            _logger = logger;
            _protector = dataProtection.CreateProtector("Nomaro.ProfileChangeRequests.v1");
        }

        #region Configuration helpers

        private string[] EntityCodes => new[]
        {
            _configuration["WorkflowEntityCodes:ProfileChange"] ?? "PROFCHG",
            _configuration["WorkflowEntityCodes:ProfileChangeIdentity"] ?? "PROFCHGID",
            _configuration["WorkflowEntityCodes:ProfileChangeBank"] ?? "PROFCHGBANK"
        };

        private string StorageRoot
        {
            get
            {
                var path = _configuration["ProfileChange:StoragePath"];
                if (string.IsNullOrWhiteSpace(path))
                    path = Path.Combine(_env.ContentRootPath, "SecureUploads", "ProfileChangeRequests");
                return path;
            }
        }

        private long MaxFileBytes => (long)(_configuration.GetValue<double?>("ProfileChange:MaxFileSizeMB") ?? 2) * 1024 * 1024;
        private int MaxFiles => _configuration.GetValue<int?>("ProfileChange:MaxFilesPerRequest") ?? 5;

        #endregion

        #region Value helpers

        private string? Protect(string? value) => string.IsNullOrEmpty(value) ? value : _protector.Protect(value);

        private string? Unprotect(string? value, bool encrypted)
        {
            if (!encrypted || string.IsNullOrEmpty(value)) return value;
            try { return _protector.Unprotect(value); }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Could not decrypt a profile change value.");
                throw new InvalidOperationException("A protected value could not be read. Contact the system administrator.");
            }
        }

        private static string? ToRaw(object? value) => value switch
        {
            null => null,
            DateTime d => d.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            decimal m => m.ToString("0.##", CultureInfo.InvariantCulture),
            double dbl => dbl.ToString("0.##", CultureInfo.InvariantCulture),
            bool b => b ? "true" : "false",
            _ => string.IsNullOrWhiteSpace(value.ToString()) ? null : value.ToString()!.Trim()
        };

        private static string? GetRaw(object? entity, string column)
        {
            if (entity == null) return null;
            var prop = entity.GetType().GetProperty(column, BindingFlags.Public | BindingFlags.Instance);
            return prop == null ? null : ToRaw(prop.GetValue(entity));
        }

        private static void SetRaw(object entity, string column, string? raw)
        {
            var prop = entity.GetType().GetProperty(column, BindingFlags.Public | BindingFlags.Instance)
                       ?? throw new InvalidOperationException($"Unknown column {column}.");
            var type = Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType;
            var nullable = !prop.PropertyType.IsValueType || Nullable.GetUnderlyingType(prop.PropertyType) != null;

            object? value;
            if (string.IsNullOrEmpty(raw))
            {
                if (type == typeof(string)) value = prop.PropertyType == typeof(string) ? (object?)null : null;
                else if (nullable) value = null;
                else if (type == typeof(decimal)) value = 0m;
                else if (type == typeof(int)) value = 0;
                else if (type == typeof(bool)) value = false;
                else throw new InvalidOperationException($"{column} cannot be empty.");
            }
            else if (type == typeof(string)) value = raw;
            else if (type == typeof(DateTime)) value = DateTime.ParseExact(raw, "yyyy-MM-dd", CultureInfo.InvariantCulture);
            else if (type == typeof(int)) value = int.Parse(raw, CultureInfo.InvariantCulture);
            else if (type == typeof(decimal)) value = decimal.Parse(raw, CultureInfo.InvariantCulture);
            else if (type == typeof(bool)) value = raw == "true";
            else throw new InvalidOperationException($"Unsupported column type for {column}.");

            prop.SetValue(entity, value);
        }

        private async Task LoadLookups()
        {
            _banks ??= await _db.Banks.AsNoTracking().ToDictionaryAsync(b => b.IdBank, b => b.BankName ?? "");
            _branches ??= await _db.BankBranches.AsNoTracking().ToDictionaryAsync(b => b.IdBankBranches, b => b.BranchName ?? "");
            _docTypes ??= await _db.DocumentTypes.AsNoTracking().ToDictionaryAsync(d => d.IdDocumentType, d => d.DocumentTypeName ?? "");
        }

        private string? Display(ProfileChangeField? field, string? raw)
        {
            if (string.IsNullOrEmpty(raw) || field == null) return raw;
            if (field.Sensitive) return R.Mask(raw);
            int.TryParse(raw, out var id);
            switch (field.DataType)
            {
                case "date":
                    return DateTime.TryParseExact(raw, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var d)
                        ? d.ToString("dd-MMM-yyyy", CultureInfo.InvariantCulture) : raw;
                case "bank": return _banks != null && _banks.TryGetValue(id, out var bn) ? bn : raw;
                case "branch": return _branches != null && _branches.TryGetValue(id, out var br) ? br : raw;
                case "doctype": return _docTypes != null && _docTypes.TryGetValue(id, out var dt) ? dt : raw;
                case "decimal": return raw + "%";
                case "yesno": return raw == "true" ? "Yes" : "No";
                case "relationship": return FamilyRelationships.Label(raw);
                default: return raw.Length > 200 ? raw[..200] : raw;
            }
        }

        // Validates and normalises one requested value. Returns null for "clear the value".
        private string? Normalise(ProfileChangeField f, string? input)
        {
            var v = input?.Trim();
            if (string.IsNullOrEmpty(v)) return null;
            if (v.Length > Math.Max(f.MaxLength, 10) && f.DataType != "text" && f.DataType != "email")
                throw new InvalidOperationException($"{f.Label} is too long.");

            switch (f.DataType)
            {
                case "text":
                    if (v.Length > f.MaxLength) throw new InvalidOperationException($"{f.Label} must not exceed {f.MaxLength} characters.");
                    if (f.Column == "CurrencyCode")
                    {
                        v = v.ToUpperInvariant();
                        if (!Regex.IsMatch(v, "^[A-Z]{3}$")) throw new InvalidOperationException("Currency must be a 3-letter code, like INR.");
                    }
                    if (f.Column == "BranchCode") v = v.ToUpperInvariant();
                    return v;
                case "email":
                    if (v.Length > f.MaxLength || !Regex.IsMatch(v, @"^[^\s@]+@[^\s@]+\.[^\s@]+$"))
                        throw new InvalidOperationException($"{f.Label} is not a valid email address.");
                    return v;
                case "phone":
                    if (!Regex.IsMatch(v, @"^[0-9+\- ]{6,20}$")) throw new InvalidOperationException($"{f.Label} must be 6 to 20 digits.");
                    return v;
                case "date":
                    if (!DateTime.TryParseExact(v, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var d))
                        throw new InvalidOperationException($"{f.Label} is not a valid date.");
                    if (d.Year < 1900 || d.Year > 2100) throw new InvalidOperationException($"{f.Label} is not a valid date.");
                    if (f.Column == "DateOfBirth" && d.Date > DateTime.Today) throw new InvalidOperationException("Date of birth cannot be in the future.");
                    return d.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
                case "gender":
                    v = v.ToUpperInvariant();
                    if (!R.Genders.Contains(v)) throw new InvalidOperationException("Gender must be Male, Female or Other.");
                    return v;
                case "relationship":
                    v = v.ToUpperInvariant();
                    if (!FamilyRelationships.IsValid(v)) throw new InvalidOperationException("Select a valid relationship.");
                    return v;
                case "yesno":
                    var yn = v.ToLowerInvariant();
                    if (yn is "true" or "yes" or "1") return "true";
                    if (yn is "false" or "no" or "0") return "false";
                    throw new InvalidOperationException($"{f.Label} must be Yes or No.");
                case "marital":
                    var m = R.MaritalStatuses.FirstOrDefault(x => x.Equals(v, StringComparison.OrdinalIgnoreCase));
                    return m ?? throw new InvalidOperationException("Marital status is not valid.");
                case "bank":
                    if (!int.TryParse(v, out var bank) || _banks == null || !_banks.ContainsKey(bank))
                        throw new InvalidOperationException("Select a valid bank.");
                    return bank.ToString(CultureInfo.InvariantCulture);
                case "branch":
                    if (!int.TryParse(v, out var branch) || _branches == null || !_branches.ContainsKey(branch))
                        throw new InvalidOperationException("Select a valid branch.");
                    return branch.ToString(CultureInfo.InvariantCulture);
                case "doctype":
                    if (!int.TryParse(v, out var docType) || _docTypes == null || !_docTypes.ContainsKey(docType))
                        throw new InvalidOperationException("Select a valid document type.");
                    return docType.ToString(CultureInfo.InvariantCulture);
                case "account":
                    v = v.Replace(" ", "").ToUpperInvariant();
                    if (!Regex.IsMatch(v, "^[A-Z0-9]{6,25}$")) throw new InvalidOperationException("Account number must be 6 to 25 letters or digits.");
                    return v;
                case "decimal":
                    if (!decimal.TryParse(v, NumberStyles.Number, CultureInfo.InvariantCulture, out var pct) || pct < 0 || pct > 100)
                        throw new InvalidOperationException($"{f.Label} must be between 0 and 100.");
                    return Math.Round(pct, 2).ToString("0.##", CultureInfo.InvariantCulture);
                case "pan":
                    v = v.ToUpperInvariant();
                    if (!Regex.IsMatch(v, "^[A-Z]{5}[0-9]{4}[A-Z]$")) throw new InvalidOperationException("PAN must be 10 characters, like ABCDE1234F.");
                    return v;
                case "uan":
                    if (!Regex.IsMatch(v, "^[0-9]{12}$")) throw new InvalidOperationException("UAN must be 12 digits.");
                    return v;
                case "esi":
                    if (!Regex.IsMatch(v, "^([0-9]{10}|[0-9]{17})$")) throw new InvalidOperationException("ESI number must be 10 or 17 digits.");
                    return v;
                default:
                    return v;
            }
        }

        #endregion

        #region Master record access

        private async Task<object?> LoadTarget(string table, int idEmployee, int? recordId, bool track)
        {
            switch (table)
            {
                case R.TEmployees:
                    return track
                        ? await _db.Employees.FirstOrDefaultAsync(e => e.IdEmployee == idEmployee)
                        : await _db.Employees.AsNoTracking().FirstOrDefaultAsync(e => e.IdEmployee == idEmployee);
                case R.TStatutory:
                    return track
                        ? await _db.EmployeeStatutoryDetails.FirstOrDefaultAsync(e => e.IdEmployee == idEmployee)
                        : await _db.EmployeeStatutoryDetails.AsNoTracking().FirstOrDefaultAsync(e => e.IdEmployee == idEmployee);
                case R.TFamily:
                    if (recordId == null) return null;
                    return track
                        ? await _db.EmployeeFamilyMembers.FirstOrDefaultAsync(c => c.IdEmployeeFamilyMember == recordId && c.IdEmployee == idEmployee)
                        : await _db.EmployeeFamilyMembers.AsNoTracking().FirstOrDefaultAsync(c => c.IdEmployeeFamilyMember == recordId && c.IdEmployee == idEmployee);
                case R.TBank:
                    if (recordId == null) return null;
                    return track
                        ? await _db.EmployeeBankAccounts.FirstOrDefaultAsync(b => b.IdEmployeeBankAccount == recordId && b.IdEmployee == idEmployee)
                        : await _db.EmployeeBankAccounts.AsNoTracking().FirstOrDefaultAsync(b => b.IdEmployeeBankAccount == recordId && b.IdEmployee == idEmployee);
                case R.TDocuments:
                    if (recordId == null) return null;
                    return track
                        ? await _db.EmployeeDocuments.FirstOrDefaultAsync(d => d.IdEmployeeDocument == recordId && d.IdEmployee == idEmployee)
                        : await _db.EmployeeDocuments.AsNoTracking().FirstOrDefaultAsync(d => d.IdEmployeeDocument == recordId && d.IdEmployee == idEmployee);
                default:
                    throw new InvalidOperationException("Unknown table.");
            }
        }

        private string RecordLabel(object? record)
        {
            switch (record)
            {
                case EmployeeFamilyMembers m: return $"{FamilyRelationships.Label(m.Relationship)}: {m.MemberName}";
                case EmployeeBankAccount b:
                    var bank = _banks != null && _banks.TryGetValue(b.IdBank, out var n) ? n : "Bank";
                    return $"{bank} · {R.Mask(b.AccountNumber)} ({b.SalaryPercentageDistributed:0.##}%)";
                case EmployeeDocuments d:
                    var type = _docTypes != null && _docTypes.TryGetValue(d.IdDocumentType, out var t) ? t : "Document";
                    return d.DocumentValidTill.HasValue ? $"{type} (valid till {d.DocumentValidTill:dd-MMM-yyyy})" : type;
                default: return "";
            }
        }

        #endregion

        #region Employee self-service

        public async Task<MyProfileDto?> GetMyProfile(int idEmployee)
        {
            await LoadLookups();
            var emp = await _db.Employees.AsNoTracking().FirstOrDefaultAsync(e => e.IdEmployee == idEmployee);
            if (emp == null) return null;

            var dto = new MyProfileDto { Employee = await BuildEmployeeCard(emp, includePhoto: true) };

            var statutory = await _db.EmployeeStatutoryDetails.AsNoTracking().FirstOrDefaultAsync(s => s.IdEmployee == idEmployee);
            foreach (var t in R.Types.Where(t => t.TableName == R.TEmployees || t.TableName == R.TStatutory))
            {
                object? source = t.TableName == R.TEmployees ? emp : statutory;
                foreach (var f in t.Fields)
                    dto.Values[$"{t.TableName}.{f.Column}"] = Display(f, GetRaw(source, f.Column));
            }

            dto.FamilyMembers = await _family.GetFamilyMembers(idEmployee);

            var accounts = await _db.EmployeeBankAccounts.AsNoTracking()
                .Where(b => b.IdEmployee == idEmployee)
                .OrderBy(b => b.OrderNumber)
                .ToListAsync();
            dto.BankAccounts = accounts.Select(b => new MyBankAccountDto
            {
                IdEmployeeBankAccount = b.IdEmployeeBankAccount,
                IdBank = b.IdBank,
                BankName = _banks!.TryGetValue(b.IdBank, out var bn) ? bn : null,
                IdBankBranch = b.IdBankBranch,
                BranchName = b.IdBankBranch.HasValue && _branches!.TryGetValue(b.IdBankBranch.Value, out var br) ? br : null,
                AccountNumberMasked = R.Mask(b.AccountNumber),
                BranchCode = b.BranchCode,
                CurrencyCode = b.CurrencyCode,
                SalaryPercentageDistributed = b.SalaryPercentageDistributed
            }).ToList();

            var docs = await _db.EmployeeDocuments.AsNoTracking()
                .Where(d => d.IdEmployee == idEmployee)
                .OrderByDescending(d => d.CreatedAt)
                .ToListAsync();
            dto.Documents = docs.Select(d => new MyDocumentDto
            {
                IdEmployeeDocument = d.IdEmployeeDocument,
                IdDocumentType = d.IdDocumentType,
                DocumentTypeName = _docTypes!.TryGetValue(d.IdDocumentType, out var dt) ? dt : null,
                DocumentValidTill = d.DocumentValidTill,
                Remarks = d.Remarks,
                HasFile = !string.IsNullOrWhiteSpace(d.DocumentFilePath),
                CreatedAt = d.CreatedAt
            }).ToList();

            dto.PendingFields = await (from r in _db.ProfileChangeRequests
                                       join d in _db.ProfileChangeRequestDetails on r.IdProfileChangeRequest equals d.IdProfileChangeRequest
                                       where r.IdEmployee == idEmployee && R.OpenStatuses.Contains(r.RequestStatus)
                                       select new PendingFieldDto
                                       {
                                           TableName = d.TableName,
                                           ColumnName = d.ColumnName,
                                           RecordId = d.RecordId,
                                           IdProfileChangeRequest = r.IdProfileChangeRequest,
                                           RequestNumber = r.RequestNumber,
                                           RequestStatus = r.RequestStatus
                                       }).AsNoTracking().ToListAsync();
            return dto;
        }

        private async Task<MyProfileEmployeeDto> BuildEmployeeCard(Employee emp, bool includePhoto)
        {
            var card = new MyProfileEmployeeDto
            {
                IdEmployee = emp.IdEmployee ?? 0,
                EmployeeCode = emp.EmployeeCode,
                FullName = string.Join(" ", new[] { emp.FirstName, emp.MiddleName, emp.LastName }.Where(x => !string.IsNullOrWhiteSpace(x))),
                JoiningDate = emp.JoiningDate,
                OfficialEmail = emp.EmailID
            };
            if (emp.IdDepartment.HasValue)
                card.DepartmentName = await _db.Departments.AsNoTracking().Where(d => d.IdDepartment == emp.IdDepartment).Select(d => d.DepartmentName).FirstOrDefaultAsync();
            if (emp.IdDesignation.HasValue)
                card.DesignationName = await _db.Designations.AsNoTracking().Where(d => d.IdDesignation == emp.IdDesignation).Select(d => d.DesignationName).FirstOrDefaultAsync();
            if (emp.IdGrade.HasValue)
                card.GradeName = await _db.Grades.AsNoTracking().Where(g => g.IdGrade == emp.IdGrade).Select(g => g.GradeCode + " - " + g.GradeName).FirstOrDefaultAsync();
            if (emp.ReportingTo is > 0)
                card.ReportingToName = await _db.Employees.AsNoTracking().Where(e => e.IdEmployee == emp.ReportingTo)
                    .Select(e => (e.FirstName + " " + (e.LastName ?? "")).Trim()).FirstOrDefaultAsync();

            if (includePhoto && !string.IsNullOrWhiteSpace(emp.EmployeePhotoFilePath) && File.Exists(emp.EmployeePhotoFilePath))
            {
                var info = new FileInfo(emp.EmployeePhotoFilePath);
                if (info.Length < 1024 * 1024)
                {
                    var bytes = await File.ReadAllBytesAsync(emp.EmployeePhotoFilePath);
                    var mime = info.Extension.ToLowerInvariant() == ".png" ? "image/png" : "image/jpeg";
                    card.PhotoBase64 = $"data:{mime};base64,{Convert.ToBase64String(bytes)}";
                }
            }
            return card;
        }

        public async Task<ProfileChangeLookupsDto> GetLookups(bool activeOnly)
        {
            await LoadLookups();
            var rows = await _db.ProfileChangeRequestTypes.AsNoTracking()
                .Where(t => !activeOnly || t.IsActive)
                .OrderBy(t => t.DisplayOrder).ThenBy(t => t.RequestTypeName)
                .ToListAsync();

            var result = new ProfileChangeLookupsDto();
            foreach (var row in rows)
            {
                var def = R.GetType(row.RequestTypeCode);
                if (def == null) continue;
                result.RequestTypes.Add(ToTypeDto(row, def));
            }

            result.Banks = _banks!.OrderBy(b => b.Value).Select(b => new LookupOptionDto { Value = b.Key.ToString(), Label = b.Value }).ToList();
            result.Branches = await _db.BankBranches.AsNoTracking().OrderBy(b => b.BranchName)
                .Select(b => new LookupOptionDto { Value = b.IdBankBranches.ToString(), Label = b.BranchName ?? "", ParentValue = b.IdBank.ToString() })
                .ToListAsync();
            result.DocumentTypes = _docTypes!.OrderBy(d => d.Value).Select(d => new LookupOptionDto { Value = d.Key.ToString(), Label = d.Value }).ToList();
            return result;
        }

        private static ProfileChangeRequestTypeDto ToTypeDto(ProfileChangeRequestTypes row, ProfileChangeTypeDef def) => new()
        {
            IdProfileChangeRequestType = row.IdProfileChangeRequestType,
            RequestCategory = row.RequestCategory,
            RequestTypeCode = row.RequestTypeCode,
            RequestTypeName = row.RequestTypeName,
            EntityCode = row.EntityCode,
            ProofRequired = row.ProofRequired || def.RequiresFile,
            ProofHint = row.ProofHint,
            DisplayOrder = row.DisplayOrder,
            IsActive = row.IsActive,
            TableName = def.TableName,
            Operation = def.Operation,
            NeedsRecord = def.NeedsRecord,
            Fields = def.Fields.Select(f => new ProfileChangeFieldDto
            {
                Column = f.Column,
                Label = f.Label,
                DataType = f.DataType,
                Required = f.Required,
                MaxLength = f.MaxLength,
                IsSensitive = f.Sensitive,
                Options = f.DataType == "relationship"
                    ? FamilyRelationships.All.Select(r => new LookupOptionDto { Value = r.Code, Label = r.Label }).ToList()
                    : f.DataType == "yesno"
                    ? new List<LookupOptionDto> { new() { Value = "true", Label = "Yes" }, new() { Value = "false", Label = "No" } }
                    : f.DataType == "gender"
                    ? R.Genders.Select(g => new LookupOptionDto { Value = g, Label = CultureInfo.InvariantCulture.TextInfo.ToTitleCase(g.ToLowerInvariant()) }).ToList()
                    : f.DataType == "marital"
                        ? R.MaritalStatuses.Select(m => new LookupOptionDto { Value = m, Label = m }).ToList()
                        : null
            }).ToList()
        };

        public async Task<int> SaveRequest(int idEmployee, SaveProfileChangeRequestDto dto)
        {
            await LoadLookups();
            var def = R.GetType(dto.RequestTypeCode) ?? throw new InvalidOperationException("Unknown request type.");
            var typeRow = await _db.ProfileChangeRequestTypes.FirstOrDefaultAsync(t => t.RequestTypeCode == def.TypeCode && t.IsActive)
                          ?? throw new InvalidOperationException("This request type is not available.");

            if ((dto.Reason?.Length ?? 0) > 500) throw new InvalidOperationException("Reason must not exceed 500 characters.");
            if ((dto.EmployeeResponse?.Length ?? 0) > 500) throw new InvalidOperationException("Response must not exceed 500 characters.");

            ProfileChangeRequests request;
            if (dto.IdProfileChangeRequest is > 0)
            {
                request = await _db.ProfileChangeRequests.FirstOrDefaultAsync(r => r.IdProfileChangeRequest == dto.IdProfileChangeRequest && r.IdEmployee == idEmployee)
                          ?? throw new KeyNotFoundException("Request not found.");
                if (request.RequestStatus != R.Draft && request.RequestStatus != R.Clarification)
                    throw new InvalidOperationException("Only a draft, or a request sent back for clarification, can be changed.");
                if (request.IdProfileChangeRequestType != typeRow.IdProfileChangeRequestType)
                    throw new InvalidOperationException("The request type of a saved request cannot be changed.");
            }
            else
            {
                request = new ProfileChangeRequests
                {
                    IdEmployee = idEmployee,
                    IdProfileChangeRequestType = typeRow.IdProfileChangeRequestType,
                    EntityCode = typeRow.EntityCode,
                    RequestStatus = R.Draft,
                    CreatedBy = idEmployee,
                    CreatedDateTime = DateTime.Now
                };
                await _db.ProfileChangeRequests.AddAsync(request);
            }

            // The record the change applies to (child, bank account, document) must belong to the employee
            object? current;
            if (def.NeedsRecord)
            {
                if (dto.RecordId is not > 0) throw new InvalidOperationException("Select the record to change.");
                current = await LoadTarget(def.TableName, idEmployee, dto.RecordId, track: false)
                          ?? throw new InvalidOperationException("The selected record was not found.");
                request.RecordId = dto.RecordId;
            }
            else
            {
                current = def.Operation == "UPDATE" ? await LoadTarget(def.TableName, idEmployee, null, track: false) : null;
                request.RecordId = null;
            }

            Dictionary<string, string?> values;
            try
            {
                values = string.IsNullOrWhiteSpace(dto.ValuesJson)
                    ? new Dictionary<string, string?>()
                    : JsonSerializer.Deserialize<Dictionary<string, string?>>(dto.ValuesJson) ?? new Dictionary<string, string?>();
            }
            catch (JsonException)
            {
                throw new InvalidOperationException("The requested values could not be read.");
            }
            values = new Dictionary<string, string?>(values, StringComparer.OrdinalIgnoreCase);

            var details = new List<ProfileChangeRequestDetails>();
            if (def.Operation == "REMOVE")
            {
                details.Add(new ProfileChangeRequestDetails
                {
                    TableName = def.TableName,
                    ColumnName = "*",
                    RecordId = dto.RecordId,
                    Operation = "REMOVE",
                    OldValueDisplay = Truncate(RecordLabel(current), 200),
                    NewValueDisplay = "Remove"
                });
            }
            else
            {
                foreach (var f in def.Fields)
                {
                    if (!values.TryGetValue(f.Column, out var input)) continue;
                    var newValue = Normalise(f, input);
                    var oldValue = def.Operation == "ADD" ? null : GetRaw(current, f.Column);
                    if (def.Operation == "UPDATE" && string.Equals(newValue ?? "", oldValue ?? "", StringComparison.Ordinal)) continue;
                    if (def.Operation == "ADD" && newValue == null) continue;

                    details.Add(new ProfileChangeRequestDetails
                    {
                        TableName = def.TableName,
                        ColumnName = f.Column,
                        RecordId = dto.RecordId,
                        Operation = def.Operation,
                        IsEncrypted = f.Sensitive,
                        OldValue = f.Sensitive ? Protect(oldValue) : oldValue,
                        NewValue = f.Sensitive ? Protect(newValue) : newValue,
                        OldValueDisplay = Truncate(Display(f, oldValue), 200),
                        NewValueDisplay = Truncate(Display(f, newValue), 200)
                    });
                }
            }

            request.Reason = dto.Reason?.Trim();
            if (dto.EmployeeResponse != null) request.EmployeeResponse = dto.EmployeeResponse.Trim();
            request.ModifiedBy = idEmployee;
            request.ModifiedDateTime = DateTime.Now;

            await using var tx = await _db.Database.BeginTransactionAsync();
            await _db.SaveChangesAsync();

            var existing = await _db.ProfileChangeRequestDetails.Where(d => d.IdProfileChangeRequest == request.IdProfileChangeRequest).ToListAsync();
            _db.ProfileChangeRequestDetails.RemoveRange(existing);
            foreach (var d in details) d.IdProfileChangeRequest = request.IdProfileChangeRequest;
            await _db.ProfileChangeRequestDetails.AddRangeAsync(details);

            if (dto.RemoveDocumentIds?.Any() == true)
            {
                var toRemove = await _db.ProfileChangeRequestDocuments
                    .Where(d => d.IdProfileChangeRequest == request.IdProfileChangeRequest && dto.RemoveDocumentIds.Contains(d.IdProfileChangeRequestDocument))
                    .ToListAsync();
                foreach (var d in toRemove) d.IsDeleted = true;
            }

            var files = dto.Files?.Where(f => f != null && f.Length > 0).ToList() ?? new List<IFormFile>();
            if (files.Any())
            {
                var activeCount = await _db.ProfileChangeRequestDocuments.CountAsync(d => d.IdProfileChangeRequest == request.IdProfileChangeRequest && !d.IsDeleted)
                                  - (dto.RemoveDocumentIds?.Count ?? 0);
                if (activeCount + files.Count > MaxFiles)
                    throw new InvalidOperationException($"A request can have at most {MaxFiles} documents.");

                for (var i = 0; i < files.Count; i++)
                {
                    var file = files[i];
                    var docTypeId = dto.FileDocumentTypeIds != null && i < dto.FileDocumentTypeIds.Count && dto.FileDocumentTypeIds[i] > 0
                        ? dto.FileDocumentTypeIds[i] : (int?)null;
                    if (docTypeId.HasValue && !_docTypes!.ContainsKey(docTypeId.Value))
                        throw new InvalidOperationException("Select a valid document type for each file.");
                    var stored = await StoreFile(request.IdProfileChangeRequest, file);
                    await _db.ProfileChangeRequestDocuments.AddAsync(new ProfileChangeRequestDocuments
                    {
                        IdProfileChangeRequest = request.IdProfileChangeRequest,
                        IdDocumentType = docTypeId,
                        OriginalFileName = Truncate(Path.GetFileName(file.FileName), 255)!,
                        StoredFileName = stored.storedName,
                        MimeType = stored.mime,
                        FileSize = file.Length,
                        UploadedBy = idEmployee,
                        UploadedOn = DateTime.Now
                    });
                }
            }

            await _db.SaveChangesAsync();
            await tx.CommitAsync();
            return request.IdProfileChangeRequest;
        }

        private async Task<(string storedName, string mime)> StoreFile(int idRequest, IFormFile file)
        {
            var ext = Path.GetExtension(file.FileName ?? "").ToLowerInvariant();
            if (!AllowedExtensions.Contains(ext)) throw new InvalidOperationException("Only PDF, JPG and PNG files are allowed.");
            if (file.Length > MaxFileBytes) throw new InvalidOperationException($"Each file must be {MaxFileBytes / 1024 / 1024} MB or smaller.");

            // Check the file signature, not just the extension
            var header = new byte[8];
            await using (var s = file.OpenReadStream())
            {
                var read = await s.ReadAsync(header.AsMemory(0, header.Length));
                if (read < 4) throw new InvalidOperationException("The file is empty or damaged.");
            }
            string mime;
            if (header[0] == 0x25 && header[1] == 0x50 && header[2] == 0x44 && header[3] == 0x46) mime = "application/pdf";
            else if (header[0] == 0xFF && header[1] == 0xD8 && header[2] == 0xFF) mime = "image/jpeg";
            else if (header[0] == 0x89 && header[1] == 0x50 && header[2] == 0x4E && header[3] == 0x47) mime = "image/png";
            else throw new InvalidOperationException("The file content is not a valid PDF, JPG or PNG.");

            var folder = Path.Combine(StorageRoot, idRequest.ToString(CultureInfo.InvariantCulture));
            Directory.CreateDirectory(folder);
            var storedName = $"{Guid.NewGuid():N}{ext}";
            await using (var target = new FileStream(Path.Combine(folder, storedName), FileMode.CreateNew))
                await file.CopyToAsync(target);
            return (storedName, mime);
        }

        private static string? Truncate(string? v, int max) => v == null ? null : (v.Length > max ? v[..max] : v);

        public async Task<string> SubmitRequest(int idEmployee, int idRequest)
        {
            await LoadLookups();
            var request = await _db.ProfileChangeRequests.FirstOrDefaultAsync(r => r.IdProfileChangeRequest == idRequest && r.IdEmployee == idEmployee)
                          ?? throw new KeyNotFoundException("Request not found.");
            if (request.RequestStatus != R.Draft && request.RequestStatus != R.Clarification)
                throw new InvalidOperationException("This request has already been submitted.");

            var typeRow = await _db.ProfileChangeRequestTypes.FirstAsync(t => t.IdProfileChangeRequestType == request.IdProfileChangeRequestType);
            if (!typeRow.IsActive) throw new InvalidOperationException("This request type is no longer available.");
            var def = R.GetType(typeRow.RequestTypeCode) ?? throw new InvalidOperationException("Unknown request type.");

            var details = await _db.ProfileChangeRequestDetails.Where(d => d.IdProfileChangeRequest == idRequest).ToListAsync();
            var docs = await _db.ProfileChangeRequestDocuments.Where(d => d.IdProfileChangeRequest == idRequest && !d.IsDeleted).ToListAsync();

            if (string.IsNullOrWhiteSpace(request.Reason)) throw new InvalidOperationException("Enter the reason for the change.");
            if (!details.Any() && !(def.RequiresFile && def.Operation == "UPDATE"))
                throw new InvalidOperationException("No changes entered. Enter at least one new value.");
            if (request.RequestStatus == R.Clarification && string.IsNullOrWhiteSpace(request.EmployeeResponse))
                throw new InvalidOperationException("Enter your response to HR before resubmitting.");

            // Required fields
            foreach (var f in def.Fields.Where(f => f.Required))
            {
                var d = details.FirstOrDefault(x => x.ColumnName == f.Column);
                if (def.Operation == "ADD" && d == null)
                    throw new InvalidOperationException($"{f.Label} is required.");
                if (def.Operation == "UPDATE" && d != null && string.IsNullOrEmpty(d.NewValue))
                    throw new InvalidOperationException($"{f.Label} cannot be empty.");
            }

            if ((typeRow.ProofRequired || def.RequiresFile) && !docs.Any())
                throw new InvalidOperationException(def.RequiresFile ? "Upload the document." : "Upload the supporting document before submitting.");

            // The selected record must still exist
            object? record = null;
            if (def.NeedsRecord)
                record = await LoadTarget(def.TableName, idEmployee, request.RecordId, track: false)
                         ?? throw new InvalidOperationException("The selected record no longer exists.");

            if (def.TableName == R.TBank && def.Operation != "REMOVE")
            {
                var branch = details.FirstOrDefault(d => d.ColumnName == "IdBankBranch")?.NewValue;
                var bank = details.FirstOrDefault(d => d.ColumnName == "IdBank")?.NewValue
                           ?? (record as EmployeeBankAccount)?.IdBank.ToString(CultureInfo.InvariantCulture);
                if (!string.IsNullOrEmpty(branch) && !string.IsNullOrEmpty(bank))
                {
                    var ok = await _db.BankBranches.AnyAsync(b => b.IdBankBranches == int.Parse(branch) && b.IdBank == int.Parse(bank));
                    if (!ok) throw new InvalidOperationException("The selected branch does not belong to the selected bank.");
                }
            }
            if (def.TableName == R.TBank)
                await ProjectBankAccounts(idEmployee, def, request.RecordId, details, apply: false);

            // Only one open request per field / record
            var openOther = await (from r in _db.ProfileChangeRequests
                                   join d in _db.ProfileChangeRequestDetails on r.IdProfileChangeRequest equals d.IdProfileChangeRequest
                                   where r.IdEmployee == idEmployee && r.IdProfileChangeRequest != idRequest
                                         && R.OpenStatuses.Contains(r.RequestStatus)
                                   select new { r.RequestNumber, d.TableName, d.ColumnName, d.RecordId }).ToListAsync();
            foreach (var d in details)
            {
                var clash = openOther.FirstOrDefault(o => o.TableName == d.TableName &&
                    (d.RecordId.HasValue
                        ? o.RecordId == d.RecordId && (o.ColumnName == "*" || d.ColumnName == "*" || o.ColumnName == d.ColumnName)
                        : o.RecordId == null && (d.TableName == R.TEmployees || d.TableName == R.TStatutory) && o.ColumnName == d.ColumnName));
                if (clash != null)
                    throw new InvalidOperationException($"Request {clash.RequestNumber} for this information is still open. Wait for it to be completed, or cancel it first.");
            }

            // Capture the current master values at submission (used to detect later conflicts)
            if (def.Operation == "UPDATE")
            {
                var current = await LoadTarget(def.TableName, idEmployee, request.RecordId, track: false);
                foreach (var d in details)
                {
                    var f = def.Fields.First(x => x.Column == d.ColumnName);
                    var oldRaw = GetRaw(current, d.ColumnName);
                    d.OldValue = f.Sensitive ? Protect(oldRaw) : oldRaw;
                    d.OldValueDisplay = Truncate(Display(f, oldRaw), 200);
                }
            }

            var wasClarification = request.RequestStatus == R.Clarification;
            await using var tx = await _db.Database.BeginTransactionAsync();
            try
            {
                request.RequestNumber ??= $"ESS-{request.IdProfileChangeRequest:D6}";
                request.SubmittedDate = DateTime.Now;
                request.ApproverRemarks = wasClarification ? request.ApproverRemarks : null;
                request.ModifiedBy = idEmployee;
                request.ModifiedDateTime = DateTime.Now;
                await _db.SaveChangesAsync();

                var result = await _workflow.InitiateApprovalWorkflow(request.IdProfileChangeRequest, request.EntityCode, idEmployee, "SUBMITTED", null, null);
                if (result != WorkflowStarted)
                    throw new InvalidOperationException($"The request could not be sent for approval: {result}");

                await _db.Entry(request).ReloadAsync();
                if (request.RequestStatus == R.FinalApproved)
                    throw new InvalidOperationException("No approver is set up for this request type. Please contact HR.");

                await tx.CommitAsync();
            }
            catch
            {
                await tx.RollbackAsync();
                throw;
            }

            await NotifyCurrentApprovers(request, idEmployee, typeRow.RequestTypeName, wasClarification ? "resubmitted" : "submitted");
            return $"Request {request.RequestNumber} has been submitted for approval.";
        }

        public async Task CancelRequest(int idEmployee, int idRequest)
        {
            var request = await _db.ProfileChangeRequests.FirstOrDefaultAsync(r => r.IdProfileChangeRequest == idRequest && r.IdEmployee == idEmployee)
                          ?? throw new KeyNotFoundException("Request not found.");

            if (request.RequestStatus == R.Submitted)
            {
                var pending = await GetPendingAllocation(request);
                var actedInCycle = pending != null && await _db.ApprovalWorkFlowAllocations.AnyAsync(a =>
                    a.EntityCode == request.EntityCode && a.EntityTablePrimaryKeyID == idRequest &&
                    a.CycleIndex == pending.CycleIndex && a.ActionStatus != null);
                if (pending == null || pending.LevelNumber != 1 || actedInCycle)
                    throw new InvalidOperationException("This request is already being reviewed and cannot be cancelled.");

                pending.ActionStatus = "CANCELLED";
                pending.ActionedBy = idEmployee;
                pending.ActionDate = DateTime.Now;
            }
            else if (request.RequestStatus != R.Draft && request.RequestStatus != R.Clarification)
            {
                throw new InvalidOperationException("Only a draft, a request sent back to you, or a request not yet reviewed can be cancelled.");
            }

            var notifyApprovers = request.RequestStatus == R.Submitted;
            List<int> approvers = new();
            if (notifyApprovers)
                approvers = ParseIds((await GetPendingAllocation(request))?.TargetIdEmployee);

            request.RequestStatus = R.Cancelled;
            request.ModifiedBy = idEmployee;
            request.ModifiedDateTime = DateTime.Now;
            await _db.SaveChangesAsync();

            if (notifyApprovers && approvers.Any())
                await Notify(approvers, idEmployee, request,
                    $"Profile change request {request.RequestNumber} cancelled",
                    $"Request {request.RequestNumber} was cancelled by the employee.",
                    "/dashboard/profile-change-requests");
        }

        public async Task<List<ProfileChangeRequestListDto>> GetMyRequests(int idEmployee)
        {
            var list = await BaseListQuery().Where(x => x.IdEmployee == idEmployee).ToListAsync();
            await FillLevels(list);
            return list.OrderByDescending(x => x.CreatedDateTime).ToList();
        }

        public async Task<FileDownloadDto?> GetMyEmployeeDocument(int idEmployee, int idEmployeeDocument)
        {
            var doc = await _db.EmployeeDocuments.AsNoTracking().FirstOrDefaultAsync(d => d.IdEmployeeDocument == idEmployeeDocument && d.IdEmployee == idEmployee);
            if (doc == null || string.IsNullOrWhiteSpace(doc.DocumentFilePath) || !File.Exists(doc.DocumentFilePath)) return null;
            var name = Path.GetFileName(doc.DocumentFilePath);
            var underscore = name.IndexOf('_');
            if (underscore > 0 && Guid.TryParse(name[..underscore], out _)) name = name[(underscore + 1)..];
            return new FileDownloadDto
            {
                Content = await File.ReadAllBytesAsync(doc.DocumentFilePath),
                ContentType = MimeFromName(name),
                FileName = name
            };
        }

        private static string MimeFromName(string name) => Path.GetExtension(name).ToLowerInvariant() switch
        {
            ".pdf" => "application/pdf",
            ".png" => "image/png",
            ".jpg" or ".jpeg" => "image/jpeg",
            _ => "application/octet-stream"
        };

        #endregion

        #region Shared detail + documents

        private IQueryable<ProfileChangeRequestListDto> BaseListQuery() =>
            from r in _db.ProfileChangeRequests.AsNoTracking()
            join t in _db.ProfileChangeRequestTypes.AsNoTracking() on r.IdProfileChangeRequestType equals t.IdProfileChangeRequestType
            join e in _db.Employees.AsNoTracking() on (int?)r.IdEmployee equals e.IdEmployee
            join dep in _db.Departments.AsNoTracking() on e.IdDepartment equals (int?)dep.IdDepartment into deps
            from dep in deps.DefaultIfEmpty()
            join des in _db.Designations.AsNoTracking() on e.IdDesignation equals (int?)des.IdDesignation into dess
            from des in dess.DefaultIfEmpty()
            select new ProfileChangeRequestListDto
            {
                IdProfileChangeRequest = r.IdProfileChangeRequest,
                RequestNumber = r.RequestNumber,
                IdEmployee = r.IdEmployee,
                EmployeeCode = e.EmployeeCode,
                EmployeeName = (e.FirstName + " " + (e.LastName ?? "")).Trim(),
                DepartmentName = dep != null ? dep.DepartmentName : null,
                DesignationName = des != null ? des.DesignationName : null,
                RequestCategory = t.RequestCategory,
                RequestTypeName = t.RequestTypeName,
                RequestStatus = r.RequestStatus,
                CurrentLevel = r.CurrentLevel,
                SubmittedDate = r.SubmittedDate,
                CreatedDateTime = r.CreatedDateTime,
                FinalActionDate = r.FinalActionDate
            };

        private async Task FillLevels(List<ProfileChangeRequestListDto> list)
        {
            if (!list.Any()) return;
            var levels = await TotalLevelsByCode();
            var ids = list.Select(l => l.IdProfileChangeRequest).ToList();
            var allocations = await _db.ApprovalWorkFlowAllocations.AsNoTracking()
                .Where(a => EntityCodes.Contains(a.EntityCode) && ids.Contains(a.EntityTablePrimaryKeyID))
                .Select(a => new { a.EntityTablePrimaryKeyID, a.EntityCode, a.CycleIndex, a.ActionStatus, a.SentDate })
                .ToListAsync();
            var codes = await _db.ProfileChangeRequests.AsNoTracking().Where(r => ids.Contains(r.IdProfileChangeRequest))
                .ToDictionaryAsync(r => r.IdProfileChangeRequest, r => r.EntityCode);

            foreach (var item in list)
            {
                var code = codes[item.IdProfileChangeRequest];
                item.TotalLevels = levels.TryGetValue(code, out var n) ? n : 0;
                var mine = allocations.Where(a => a.EntityTablePrimaryKeyID == item.IdProfileChangeRequest && a.EntityCode == code).ToList();
                item.IsResubmitted = mine.Any(a => a.CycleIndex > 1);
                var pending = mine.Where(a => a.ActionStatus == null).OrderByDescending(a => a.SentDate).FirstOrDefault();
                if (pending != null && R.WithApproverStatuses.Contains(item.RequestStatus))
                    item.WaitingDays = (int)(DateTime.Now - pending.SentDate).TotalDays;
            }
        }

        private async Task<Dictionary<string, int>> TotalLevelsByCode()
        {
            var codes = EntityCodes;
            return await (from c in _db.WorkFlowConfig.AsNoTracking()
                          join d in _db.WorkFlowConfigDetails.AsNoTracking() on c.IdWorkFlowConfig equals d.IdWorkFlowConfig
                          where codes.Contains(c.EntityCode)
                          group d by c.EntityCode into g
                          select new { g.Key, Count = g.Count() }).ToDictionaryAsync(x => x.Key, x => x.Count);
        }

        private async Task<ApprovalWorkFlowAllocation?> GetPendingAllocation(ProfileChangeRequests request) =>
            await _db.ApprovalWorkFlowAllocations
                .Where(a => a.EntityCode == request.EntityCode && a.EntityTablePrimaryKeyID == request.IdProfileChangeRequest && a.ActionStatus == null)
                .OrderByDescending(a => a.CycleIndex).ThenByDescending(a => a.LevelNumber)
                .FirstOrDefaultAsync();

        private static List<int> ParseIds(string? csv) =>
            (csv ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(x => int.TryParse(x, out var id) ? id : 0).Where(id => id > 0).Distinct().ToList();

        private async Task<bool> IsInvolvedApprover(ProfileChangeRequests request, int viewerId)
        {
            var allocations = await _db.ApprovalWorkFlowAllocations.AsNoTracking()
                .Where(a => a.EntityCode == request.EntityCode && a.EntityTablePrimaryKeyID == request.IdProfileChangeRequest)
                .Select(a => new { a.TargetIdEmployee, a.ActionedBy }).ToListAsync();
            return allocations.Any(a => a.ActionedBy == viewerId || ParseIds(a.TargetIdEmployee).Contains(viewerId));
        }

        public async Task<ProfileChangeRequestDetailDto?> GetRequestDetail(int idRequest, int viewerId, bool canViewAll, bool canReveal)
        {
            await LoadLookups();
            var request = await _db.ProfileChangeRequests.AsNoTracking().FirstOrDefaultAsync(r => r.IdProfileChangeRequest == idRequest);
            if (request == null) return null;

            var isOwner = request.IdEmployee == viewerId;
            var isHr = !isOwner && (canViewAll || await IsInvolvedApprover(request, viewerId));
            if (!isOwner && !isHr) return null;
            if (!isOwner && request.RequestStatus == R.Draft) return null;

            var typeRow = await _db.ProfileChangeRequestTypes.AsNoTracking().FirstAsync(t => t.IdProfileChangeRequestType == request.IdProfileChangeRequestType);
            var def = R.GetType(typeRow.RequestTypeCode)!;
            var emp = await _db.Employees.AsNoTracking().FirstAsync(e => e.IdEmployee == request.IdEmployee);
            var levels = await TotalLevelsByCode();
            var pending = await GetPendingAllocation(request);

            var dto = new ProfileChangeRequestDetailDto
            {
                IdProfileChangeRequest = request.IdProfileChangeRequest,
                RequestNumber = request.RequestNumber,
                RequestTypeCode = typeRow.RequestTypeCode,
                RequestTypeName = typeRow.RequestTypeName,
                RequestCategory = typeRow.RequestCategory,
                EntityCode = request.EntityCode,
                TableName = def.TableName,
                Operation = def.Operation,
                ProofRequired = typeRow.ProofRequired || def.RequiresFile,
                ProofHint = typeRow.ProofHint,
                RecordId = request.RecordId,
                RequestStatus = request.RequestStatus,
                CurrentLevel = request.CurrentLevel,
                TotalLevels = levels.TryGetValue(request.EntityCode, out var n) ? n : 0,
                Reason = request.Reason,
                ApproverRemarks = request.ApproverRemarks,
                EmployeeResponse = request.EmployeeResponse,
                CreatedDateTime = request.CreatedDateTime,
                SubmittedDate = request.SubmittedDate,
                FinalActionDate = request.FinalActionDate,
                Employee = await BuildEmployeeCard(emp, includePhoto: false),
                CanRevealSensitive = isHr && canReveal
            };

            object? currentRecord = def.NeedsRecord || def.Operation == "UPDATE"
                ? await LoadTarget(def.TableName, request.IdEmployee, request.RecordId, track: false) : null;
            if (def.NeedsRecord) dto.RecordLabel = currentRecord != null ? RecordLabel(currentRecord) : "(record no longer exists)";

            var editable = isOwner && (request.RequestStatus == R.Draft || request.RequestStatus == R.Clarification);
            var details = await _db.ProfileChangeRequestDetails.AsNoTracking()
                .Where(d => d.IdProfileChangeRequest == idRequest).OrderBy(d => d.IdProfileChangeRequestDetail).ToListAsync();
            var checkConflicts = R.WithApproverStatuses.Contains(request.RequestStatus);
            foreach (var d in details)
            {
                var f = def.Fields.FirstOrDefault(x => x.Column == d.ColumnName);
                var row = new ProfileChangeRowDto
                {
                    IdProfileChangeRequestDetail = d.IdProfileChangeRequestDetail,
                    ColumnName = d.ColumnName,
                    Label = f?.Label ?? (d.Operation == "REMOVE" ? "Record" : d.ColumnName),
                    Operation = d.Operation,
                    OldValue = d.OldValueDisplay,
                    NewValue = d.NewValueDisplay,
                    IsSensitive = d.IsEncrypted
                };
                if (editable && d.Operation != "REMOVE") row.NewValueRaw = Unprotect(d.NewValue, d.IsEncrypted);

                if (isHr && checkConflicts && d.Operation != "ADD")
                {
                    if (d.Operation == "REMOVE")
                    {
                        row.CurrentValue = currentRecord != null ? RecordLabel(currentRecord) : "(not found)";
                        row.HasConflict = currentRecord == null;
                    }
                    else
                    {
                        var currentRaw = GetRaw(currentRecord, d.ColumnName);
                        row.CurrentValue = Display(f, currentRaw);
                        var oldRaw = Unprotect(d.OldValue, d.IsEncrypted);
                        row.HasConflict = (def.NeedsRecord && currentRecord == null) || !string.Equals(currentRaw ?? "", oldRaw ?? "", StringComparison.Ordinal);
                    }
                }
                dto.Changes.Add(row);
            }
            dto.HasConflicts = dto.Changes.Any(c => c.HasConflict);

            dto.Documents = await (from d in _db.ProfileChangeRequestDocuments.AsNoTracking()
                                   where d.IdProfileChangeRequest == idRequest && !d.IsDeleted
                                   orderby d.UploadedOn
                                   select new ProfileChangeDocumentDto
                                   {
                                       IdProfileChangeRequestDocument = d.IdProfileChangeRequestDocument,
                                       IdDocumentType = d.IdDocumentType,
                                       OriginalFileName = d.OriginalFileName,
                                       MimeType = d.MimeType,
                                       FileSize = d.FileSize,
                                       UploadedOn = d.UploadedOn
                                   }).ToListAsync();
            foreach (var doc in dto.Documents)
                if (doc.IdDocumentType.HasValue && _docTypes!.TryGetValue(doc.IdDocumentType.Value, out var dn)) doc.DocumentTypeName = dn;

            dto.History = await BuildHistory(request);

            dto.CanEdit = editable;
            dto.CanCancel = isOwner && (request.RequestStatus == R.Draft || request.RequestStatus == R.Clarification ||
                            (request.RequestStatus == R.Submitted && pending != null && pending.LevelNumber == 1 &&
                             !await _db.ApprovalWorkFlowAllocations.AnyAsync(a => a.EntityCode == request.EntityCode &&
                                 a.EntityTablePrimaryKeyID == idRequest && a.CycleIndex == pending.CycleIndex && a.ActionStatus != null)));
            dto.CanAct = isHr && pending != null && R.WithApproverStatuses.Contains(request.RequestStatus)
                         && ParseIds(pending.TargetIdEmployee).Contains(viewerId) && request.IdEmployee != viewerId;
            dto.IsFinalLevel = pending != null && dto.TotalLevels > 0 && pending.LevelNumber >= dto.TotalLevels;
            return dto;
        }

        private async Task<List<ProfileChangeHistoryDto>> BuildHistory(ProfileChangeRequests request)
        {
            var allocations = await _db.ApprovalWorkFlowAllocations.AsNoTracking()
                .Where(a => a.EntityCode == request.EntityCode && a.EntityTablePrimaryKeyID == request.IdProfileChangeRequest)
                .OrderBy(a => a.CycleIndex).ThenBy(a => a.LevelNumber).ThenBy(a => a.SentDate)
                .ToListAsync();

            var ids = allocations.SelectMany(a => ParseIds(a.TargetIdEmployee).Append(a.ActionedBy ?? 0).Append(a.SourceIdEmployee))
                .Append(request.IdEmployee).Where(i => i > 0).Distinct().ToList();
            var names = await _db.Employees.AsNoTracking().Where(e => e.IdEmployee.HasValue && ids.Contains(e.IdEmployee.Value))
                .ToDictionaryAsync(e => e.IdEmployee!.Value, e => (e.FirstName + " " + (e.LastName ?? "")).Trim());
            string Name(int? id) => id.HasValue && names.TryGetValue(id.Value, out var nm) ? nm : "";

            var history = new List<ProfileChangeHistoryDto>
            {
                new() { ActionDate = request.CreatedDateTime, Action = "Request created", ActionBy = Name(request.IdEmployee) }
            };
            foreach (var a in allocations)
            {
                if (a.LevelNumber == 1)
                    history.Add(new ProfileChangeHistoryDto
                    {
                        ActionDate = a.SentDate,
                        Action = a.CycleIndex > 1 ? $"Resubmitted (cycle {a.CycleIndex})" : "Submitted",
                        ActionBy = Name(request.IdEmployee),
                        CycleIndex = a.CycleIndex
                    });

                if (a.ActionStatus == null)
                {
                    history.Add(new ProfileChangeHistoryDto
                    {
                        Action = $"Waiting for level {a.LevelNumber} approval",
                        ActionBy = string.Join(", ", ParseIds(a.TargetIdEmployee).Select(i => Name(i)).Where(x => x != "")),
                        LevelNumber = a.LevelNumber,
                        CycleIndex = a.CycleIndex,
                        IsPending = true
                    });
                    continue;
                }

                var action = a.ActionStatus switch
                {
                    "REJECTED" => $"Rejected at level {a.LevelNumber}",
                    "CLARIFICATION" => $"Sent back for clarification at level {a.LevelNumber}",
                    "CANCELLED" => "Cancelled by employee",
                    _ => $"Approved at level {a.LevelNumber}"
                };
                history.Add(new ProfileChangeHistoryDto
                {
                    ActionDate = a.ActionDate,
                    Action = action,
                    ActionBy = Name(a.ActionedBy),
                    LevelNumber = a.LevelNumber,
                    CycleIndex = a.CycleIndex,
                    Remarks = a.RejectionRemarks
                });
            }
            if (request.AppliedDate.HasValue)
                history.Add(new ProfileChangeHistoryDto { ActionDate = request.AppliedDate, Action = "Changes applied to employee records" });
            if (request.RequestStatus == R.Cancelled && !allocations.Any(a => a.ActionStatus == "CANCELLED"))
                history.Add(new ProfileChangeHistoryDto { ActionDate = request.ModifiedDateTime, Action = "Cancelled by employee", ActionBy = Name(request.IdEmployee) });
            return history;
        }

        public async Task<FileDownloadDto?> GetRequestDocument(int idRequestDocument, int viewerId, bool canViewAll)
        {
            var doc = await _db.ProfileChangeRequestDocuments.AsNoTracking().FirstOrDefaultAsync(d => d.IdProfileChangeRequestDocument == idRequestDocument && !d.IsDeleted);
            if (doc == null) return null;
            var request = await _db.ProfileChangeRequests.AsNoTracking().FirstAsync(r => r.IdProfileChangeRequest == doc.IdProfileChangeRequest);
            var allowed = request.IdEmployee == viewerId ||
                          (request.RequestStatus != R.Draft && (canViewAll || await IsInvolvedApprover(request, viewerId)));
            if (!allowed) return null;

            var path = Path.Combine(StorageRoot, doc.IdProfileChangeRequest.ToString(CultureInfo.InvariantCulture), doc.StoredFileName);
            if (!File.Exists(path)) return null;
            return new FileDownloadDto
            {
                Content = await File.ReadAllBytesAsync(path),
                ContentType = doc.MimeType ?? MimeFromName(doc.StoredFileName),
                FileName = doc.OriginalFileName
            };
        }

        #endregion

        #region HR

        private async Task<List<int>> RequestsWaitingFor(int viewerId)
        {
            var codes = EntityCodes;
            var pending = await _db.ApprovalWorkFlowAllocations.AsNoTracking()
                .Where(a => codes.Contains(a.EntityCode) && a.ActionStatus == null)
                .Select(a => new { a.EntityTablePrimaryKeyID, a.TargetIdEmployee })
                .ToListAsync();
            var ids = pending.Where(p => ParseIds(p.TargetIdEmployee).Contains(viewerId)).Select(p => p.EntityTablePrimaryKeyID).Distinct().ToList();
            return await _db.ProfileChangeRequests.AsNoTracking()
                .Where(r => ids.Contains(r.IdProfileChangeRequest) && R.WithApproverStatuses.Contains(r.RequestStatus) && r.IdEmployee != viewerId)
                .Select(r => r.IdProfileChangeRequest).ToListAsync();
        }

        public async Task<ProfileChangeDashboardDto> GetDashboard(int viewerId, bool canViewAll)
        {
            var since = DateTime.Now.AddDays(-30);
            var dto = new ProfileChangeDashboardDto { WaitingForMe = (await RequestsWaitingFor(viewerId)).Count };
            var q = _db.ProfileChangeRequests.AsNoTracking();
            if (canViewAll)
            {
                dto.WithEmployee = await q.CountAsync(r => r.RequestStatus == R.Clarification);
                dto.Approved30Days = await q.CountAsync(r => r.RequestStatus == R.FinalApproved && r.FinalActionDate >= since);
                dto.Rejected30Days = await q.CountAsync(r => r.RequestStatus == R.Rejected && r.FinalActionDate >= since);
            }
            else
            {
                dto.Approved30Days = await q.CountAsync(r => r.RequestStatus == R.FinalApproved && r.FinalActionBy == viewerId && r.FinalActionDate >= since);
                dto.Rejected30Days = await q.CountAsync(r => r.RequestStatus == R.Rejected && r.FinalActionBy == viewerId && r.FinalActionDate >= since);
            }
            return dto;
        }

        public async Task<List<ProfileChangeRequestListDto>> GetHrRequests(int viewerId, bool canViewAll, ProfileChangeHrFilterDto filter)
        {
            var query = BaseListQuery();
            var mine = !canViewAll || !string.Equals(filter.Mode, "ALL", StringComparison.OrdinalIgnoreCase);
            if (mine)
            {
                var ids = await RequestsWaitingFor(viewerId);
                query = query.Where(x => ids.Contains(x.IdProfileChangeRequest));
            }
            else
            {
                query = query.Where(x => x.RequestStatus != R.Draft);
                if (!string.IsNullOrWhiteSpace(filter.Status))
                {
                    if (filter.Status == "PENDING") query = query.Where(x => R.WithApproverStatuses.Contains(x.RequestStatus));
                    else query = query.Where(x => x.RequestStatus == filter.Status);
                }
            }

            if (!string.IsNullOrWhiteSpace(filter.Category)) query = query.Where(x => x.RequestCategory == filter.Category);
            if (filter.IdDepartment is > 0)
            {
                var empIds = _db.Employees.Where(e => e.IdDepartment == filter.IdDepartment).Select(e => e.IdEmployee);
                query = query.Where(x => empIds.Contains(x.IdEmployee));
            }
            if (filter.FromDate.HasValue) query = query.Where(x => (x.SubmittedDate ?? x.CreatedDateTime) >= filter.FromDate.Value.Date);
            if (filter.ToDate.HasValue) query = query.Where(x => (x.SubmittedDate ?? x.CreatedDateTime) < filter.ToDate.Value.Date.AddDays(1));
            if (!string.IsNullOrWhiteSpace(filter.Search))
            {
                var s = filter.Search.Trim();
                query = query.Where(x => (x.RequestNumber != null && x.RequestNumber.Contains(s)) ||
                                         (x.EmployeeCode != null && x.EmployeeCode.Contains(s)) ||
                                         x.EmployeeName!.Contains(s));
            }

            var list = await query.OrderByDescending(x => x.SubmittedDate).Take(500).ToListAsync();
            await FillLevels(list);
            return list;
        }

        public async Task<string> ActionRequest(int approverId, ProfileChangeActionDto dto)
        {
            var action = (dto.Action ?? "").Trim().ToUpperInvariant();
            if (action != "APPROVE" && action != "REJECT" && action != "CLARIFY")
                throw new InvalidOperationException("Unknown action.");
            var remarks = dto.Remarks?.Trim();
            if ((action == "REJECT" || action == "CLARIFY") && string.IsNullOrWhiteSpace(remarks))
                throw new InvalidOperationException(action == "REJECT" ? "Enter the reason for rejection." : "Enter what the employee needs to provide.");
            if ((remarks?.Length ?? 0) > 500) throw new InvalidOperationException("Remarks must not exceed 500 characters.");

            var request = await _db.ProfileChangeRequests.FirstOrDefaultAsync(r => r.IdProfileChangeRequest == dto.IdProfileChangeRequest)
                          ?? throw new KeyNotFoundException("Request not found.");
            if (!R.WithApproverStatuses.Contains(request.RequestStatus))
                throw new InvalidOperationException("This request is not waiting for approval.");
            if (request.IdEmployee == approverId)
                throw new InvalidOperationException("You cannot approve your own request.");

            var pending = await GetPendingAllocation(request);
            if (pending == null || !ParseIds(pending.TargetIdEmployee).Contains(approverId))
                throw new InvalidOperationException("You are not the approver for this request at its current level.");

            var typeRow = await _db.ProfileChangeRequestTypes.AsNoTracking().FirstAsync(t => t.IdProfileChangeRequestType == request.IdProfileChangeRequestType);

            if (action == "APPROVE" && !dto.ConfirmConflicts)
            {
                var detail = await GetRequestDetail(request.IdProfileChangeRequest, approverId, true, false);
                if (detail?.HasConflicts == true)
                    throw new ProfileChangeConflictException("The employee's record has changed since this request was submitted. Review the highlighted fields and confirm to approve.");
            }

            await using var tx = await _db.Database.BeginTransactionAsync();
            try
            {
                if (action == "CLARIFY")
                {
                    pending.ActionStatus = "CLARIFICATION";
                    pending.ActionedBy = approverId;
                    pending.ActionDate = DateTime.Now;
                    pending.RejectionRemarks = remarks;
                    request.RequestStatus = R.Clarification;
                    request.ApproverRemarks = remarks;
                    request.EmployeeResponse = null;
                }
                else if (action == "REJECT")
                {
                    var result = await _workflow.InitiateApprovalWorkflow(request.IdProfileChangeRequest, request.EntityCode, approverId, "REJECTED", null, remarks);
                    if (result != WorkflowRejected) throw new InvalidOperationException(result);
                    await _db.Entry(request).ReloadAsync();
                    request.RequestStatus = R.Rejected;
                    request.ApproverRemarks = remarks;
                    request.FinalActionBy = approverId;
                    request.FinalActionDate = DateTime.Now;
                }
                else
                {
                    var result = await _workflow.InitiateApprovalWorkflow(request.IdProfileChangeRequest, request.EntityCode, approverId, "APPROVED", null, null);
                    if (result != WorkflowApproved) throw new InvalidOperationException(result);
                    await _db.Entry(request).ReloadAsync();

                    if (!string.IsNullOrWhiteSpace(remarks))
                    {
                        var acted = await _db.ApprovalWorkFlowAllocations.FirstAsync(a => a.IdApprovalWorkFlow == pending.IdApprovalWorkFlow);
                        acted.RejectionRemarks = remarks;
                    }

                    if (request.RequestStatus == R.FinalApproved)
                    {
                        if (request.AppliedDate.HasValue) throw new InvalidOperationException("This request has already been applied.");
                        await ApplyChanges(request, typeRow, approverId);
                        request.FinalActionBy = approverId;
                        request.FinalActionDate = DateTime.Now;
                        request.ApproverRemarks = remarks;
                    }
                }

                request.ModifiedBy = approverId;
                request.ModifiedDateTime = DateTime.Now;
                await _db.SaveChangesAsync();
                await tx.CommitAsync();
            }
            catch (Exception ex)
            {
                await tx.RollbackAsync();
                _db.ChangeTracker.Clear();
                _logger.LogError(ex, "Profile change request {Id}: {Action} failed.", dto.IdProfileChangeRequest, action);
                if (ex is InvalidOperationException) throw;
                // show the underlying (usually database) error so the administrator can see what failed
                var root = ex;
                while (root.InnerException != null) root = root.InnerException;
                var detail = root.Message.Length > 300 ? root.Message[..300] + "..." : root.Message;
                throw new InvalidOperationException($"The change could not be applied. Nothing was changed. Details: {detail}");
            }

            // Notifications after the commit
            if (action == "CLARIFY")
            {
                await Notify(new List<int> { request.IdEmployee }, approverId, request,
                    $"Clarification needed: {request.RequestNumber}",
                    $"HR needs more information for your request {request.RequestNumber} ({typeRow.RequestTypeName}). Open My Change Requests to respond.",
                    "/dashboard/my-change-requests");
                return $"Request {request.RequestNumber} has been sent back to the employee.";
            }
            if (action == "REJECT")
            {
                await Notify(new List<int> { request.IdEmployee }, approverId, request,
                    $"Request {request.RequestNumber} rejected",
                    $"Your request {request.RequestNumber} ({typeRow.RequestTypeName}) was rejected. Open My Change Requests to see the reason.",
                    "/dashboard/my-change-requests");
                return $"Request {request.RequestNumber} has been rejected.";
            }
            if (request.RequestStatus == R.FinalApproved)
            {
                await Notify(new List<int> { request.IdEmployee }, approverId, request,
                    $"Request {request.RequestNumber} approved",
                    $"Your request {request.RequestNumber} ({typeRow.RequestTypeName}) was approved and your profile has been updated.",
                    "/dashboard/my-change-requests");
                return $"Request {request.RequestNumber} approved. The employee's records have been updated.";
            }
            await NotifyCurrentApprovers(request, approverId, typeRow.RequestTypeName, "approved at the previous level and is waiting for you");
            return $"Request {request.RequestNumber} approved and sent to the next level.";
        }

        public async Task<string?> RevealValue(int idDetail, string which, int viewerId, bool canViewAll)
        {
            var detail = await _db.ProfileChangeRequestDetails.AsNoTracking().FirstOrDefaultAsync(d => d.IdProfileChangeRequestDetail == idDetail);
            if (detail == null) return null;
            var request = await _db.ProfileChangeRequests.AsNoTracking().FirstAsync(r => r.IdProfileChangeRequest == detail.IdProfileChangeRequest);
            if (request.RequestStatus == R.Draft || (!canViewAll && !await IsInvolvedApprover(request, viewerId))) return null;

            var value = Unprotect(string.Equals(which, "OLD", StringComparison.OrdinalIgnoreCase) ? detail.OldValue : detail.NewValue, detail.IsEncrypted);
            await _audit.LogAuditAsync("View", "ProfileChangeSensitiveValue", idDetail,
                new { request.RequestNumber, detail.ColumnName, Which = which, ViewedBy = viewerId });
            return value;
        }

        public async Task<List<ProfileChangeHistoryEntryDto>> GetEmployeeChangeHistory(int idEmployee)
        {
            var rows = await _db.AuditLog.AsNoTracking()
                .Where(a => a.EntityName == "EmployeeProfileChange" && a.EntityId == idEmployee)
                .OrderByDescending(a => a.ActionTimestamp).Take(200).ToListAsync();
            var userIds = rows.Select(r => int.TryParse(r.ActionBy, out var id) ? id : 0).Where(i => i > 0).Distinct().ToList();
            var names = await _db.Employees.AsNoTracking().Where(e => e.IdEmployee.HasValue && userIds.Contains(e.IdEmployee.Value))
                .ToDictionaryAsync(e => e.IdEmployee!.Value.ToString(), e => (e.FirstName + " " + (e.LastName ?? "")).Trim());
            return rows.Select(r => new ProfileChangeHistoryEntryDto
            {
                ActionTimestamp = r.ActionTimestamp,
                ActionBy = names.TryGetValue(r.ActionBy ?? "", out var n) ? n : r.ActionBy,
                Details = r.ActionDetails
            }).ToList();
        }

        #endregion

        #region Apply approved changes

        // EmployeeStatutoryDetails.Gender holds one letter (M / F / O), as EmployeeStatutoryDetailsService saves it
        private static string? StatutoryGender(string? gender) =>
            string.IsNullOrWhiteSpace(gender) ? null : gender.Trim().Substring(0, 1).ToUpperInvariant();

        private async Task ApplyChanges(ProfileChangeRequests request, ProfileChangeRequestTypes typeRow, int approverId)
        {
            await LoadLookups();
            var def = R.GetType(typeRow.RequestTypeCode) ?? throw new InvalidOperationException("Unknown request type.");
            var details = await _db.ProfileChangeRequestDetails.Where(d => d.IdProfileChangeRequest == request.IdProfileChangeRequest).ToListAsync();
            var docs = await _db.ProfileChangeRequestDocuments.Where(d => d.IdProfileChangeRequest == request.IdProfileChangeRequest && !d.IsDeleted)
                .OrderByDescending(d => d.UploadedOn).ToListAsync();
            var idEmployee = request.IdEmployee;
            var values = details.Where(d => d.ColumnName != "*")
                .ToDictionary(d => d.ColumnName, d => Unprotect(d.NewValue, d.IsEncrypted));

            switch (def.TableName)
            {
                case R.TEmployees:
                {
                    var emp = (Employee?)await LoadTarget(R.TEmployees, idEmployee, null, true) ?? throw new InvalidOperationException("Employee not found.");
                    foreach (var kv in values) SetRaw(emp, kv.Key, kv.Value);
                    if (values.ContainsKey("DateOfBirth") || values.ContainsKey("Gender"))
                    {
                        var st = await _db.EmployeeStatutoryDetails.FirstOrDefaultAsync(s => s.IdEmployee == idEmployee);
                        if (st != null)
                        {
                            if (values.ContainsKey("DateOfBirth")) st.DateOfBirth = emp.DateOfBirth;
                            if (values.ContainsKey("Gender")) st.Gender = StatutoryGender(emp.Gender);
                            st.ModifiedBy = approverId;
                            st.ModifiedOn = DateTime.Now;
                        }
                    }
                    break;
                }
                case R.TStatutory:
                {
                    var st = await _db.EmployeeStatutoryDetails.FirstOrDefaultAsync(s => s.IdEmployee == idEmployee);
                    if (st == null)
                    {
                        var emp = await _db.Employees.AsNoTracking().FirstAsync(e => e.IdEmployee == idEmployee);
                        st = new EmployeeStatutoryDetails
                        {
                            IdEmployee = idEmployee,
                            Gender = StatutoryGender(emp.Gender),
                            DateOfBirth = emp.DateOfBirth,
                            CreatedBy = approverId,
                            CreatedOn = DateTime.Now
                        };
                        await _db.EmployeeStatutoryDetails.AddAsync(st);
                    }
                    foreach (var kv in values) SetRaw(st, kv.Key, kv.Value);
                    st.ModifiedBy = approverId;
                    st.ModifiedOn = DateTime.Now;
                    break;
                }
                case R.TFamily:
                {
                    var family = await _db.EmployeeFamilyMembers.Where(m => m.IdEmployee == idEmployee).ToListAsync();
                    if (def.Operation == "ADD")
                    {
                        var member = new EmployeeFamilyMembers { IdEmployee = idEmployee, Relationship = "", MemberName = "", CreatedBy = approverId, CreatedDateTime = DateTime.Now };
                        foreach (var kv in values) SetRaw(member, kv.Key, kv.Value);
                        member.Gender ??= FamilyRelationships.DefaultGender(member.Relationship);
                        family.Add(member);
                        FamilyRelationships.Validate(family);
                        await _db.EmployeeFamilyMembers.AddAsync(member);
                    }
                    else
                    {
                        var member = family.FirstOrDefault(m => m.IdEmployeeFamilyMember == request.RecordId)
                                     ?? throw new InvalidOperationException("The family member record no longer exists.");
                        if (def.Operation == "REMOVE")
                        {
                            _db.EmployeeFamilyMembers.Remove(member);
                        }
                        else
                        {
                            foreach (var kv in values) SetRaw(member, kv.Key, kv.Value);
                            member.ModifiedBy = approverId;
                            member.ModifiedDateTime = DateTime.Now;
                            FamilyRelationships.Validate(family);
                        }
                    }
                    await _db.SaveChangesAsync();
                    await _family.UpdateChildrenCount(idEmployee);
                    break;
                }
                case R.TBank:
                    await ProjectBankAccounts(idEmployee, def, request.RecordId, details, apply: true);
                    break;
                case R.TDocuments:
                {
                    var file = docs.FirstOrDefault() ?? throw new InvalidOperationException("The document file is missing.");
                    var path = CopyToEmployeeDocuments(file);
                    EmployeeDocuments target;
                    if (def.Operation == "ADD")
                    {
                        target = new EmployeeDocuments
                        {
                            IdEmployee = idEmployee,
                            IdDocumentType = int.Parse(values["IdDocumentType"]!, CultureInfo.InvariantCulture),
                            CreatedAt = DateTime.Now,
                            CreatedBy = approverId
                        };
                        await _db.EmployeeDocuments.AddAsync(target);
                    }
                    else
                    {
                        target = (EmployeeDocuments?)await LoadTarget(R.TDocuments, idEmployee, request.RecordId, true)
                                 ?? throw new InvalidOperationException("The document record no longer exists.");
                        target.UpdatedAt = DateTime.Now;
                        target.UpdatedBy = approverId;
                    }
                    foreach (var kv in values.Where(v => v.Key != "IdDocumentType")) SetRaw(target, kv.Key, kv.Value);
                    target.DocumentFilePath = path;
                    await _db.SaveChangesAsync();
                    file.IdEmployeeDocument = target.IdEmployeeDocument;
                    break;
                }
            }

            // Proofs with a document type become employee documents too (passport copy, address proof...)
            if (def.TableName != R.TDocuments)
            {
                foreach (var proof in docs.Where(d => d.IdDocumentType.HasValue))
                {
                    var copy = new EmployeeDocuments
                    {
                        IdEmployee = idEmployee,
                        IdDocumentType = proof.IdDocumentType!.Value,
                        Remarks = Truncate($"{request.RequestNumber} - {typeRow.RequestTypeName}", 250),
                        DocumentFilePath = CopyToEmployeeDocuments(proof),
                        CreatedAt = DateTime.Now,
                        CreatedBy = approverId
                    };
                    await _db.EmployeeDocuments.AddAsync(copy);
                    await _db.SaveChangesAsync();
                    proof.IdEmployeeDocument = copy.IdEmployeeDocument;
                }
            }

            request.AppliedDate = DateTime.Now;
            await _db.SaveChangesAsync();

            await _audit.LogAuditAsync("Update", "EmployeeProfileChange", idEmployee, new
            {
                request.IdProfileChangeRequest,
                request.RequestNumber,
                RequestType = typeRow.RequestTypeName,
                ApprovedBy = approverId,
                Changes = details.Select(d => new
                {
                    Field = def.Fields.FirstOrDefault(f => f.Column == d.ColumnName)?.Label ?? d.ColumnName,
                    d.Operation,
                    From = d.OldValueDisplay,
                    To = d.NewValueDisplay
                })
            });
        }

        // Same folder and naming as the Employee Documents tab, so HR sees the file there.
        private string CopyToEmployeeDocuments(ProfileChangeRequestDocuments doc)
        {
            var source = Path.Combine(StorageRoot, doc.IdProfileChangeRequest.ToString(CultureInfo.InvariantCulture), doc.StoredFileName);
            if (!File.Exists(source)) throw new InvalidOperationException($"The file {doc.OriginalFileName} is missing on the server.");
            var folder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "employeedocuments");
            Directory.CreateDirectory(folder);
            var safeName = string.Concat(Path.GetFileName(doc.OriginalFileName).Select(c => Path.GetInvalidFileNameChars().Contains(c) ? '_' : c));
            var target = Path.Combine(folder, $"{Guid.NewGuid()}_{safeName}");
            File.Copy(source, target);
            return target;
        }

        /// <summary>
        /// Works out the employee's bank accounts after the change. Rules:
        /// an account set to 100% makes every other account 0%; when one account is left it gets 100%;
        /// otherwise the salary % must total 100.
        /// </summary>
        private async Task ProjectBankAccounts(int idEmployee, ProfileChangeTypeDef def, int? recordId, List<ProfileChangeRequestDetails> details, bool apply)
        {
            var accounts = apply
                ? await _db.EmployeeBankAccounts.Where(b => b.IdEmployee == idEmployee).ToListAsync()
                : await _db.EmployeeBankAccounts.AsNoTracking().Where(b => b.IdEmployee == idEmployee).ToListAsync();
            var values = details.Where(d => d.ColumnName != "*").ToDictionary(d => d.ColumnName, d => Unprotect(d.NewValue, d.IsEncrypted));

            EmployeeBankAccount? changed = null;
            if (def.Operation == "ADD")
            {
                changed = new EmployeeBankAccount
                {
                    IdEmployee = idEmployee,
                    AccountNumber = "",
                    BranchCode = "",
                    DisbursementType = accounts.FirstOrDefault()?.DisbursementType ?? "PERCENTAGE",
                    CurrencyCode = accounts.FirstOrDefault()?.CurrencyCode ?? "INR",
                    OrderNumber = (accounts.Max(a => a.OrderNumber) ?? 0) + 1
                };
                foreach (var kv in values) SetRaw(changed, kv.Key, kv.Value);
                changed.BranchCode ??= "";
                changed.CurrencyCode = string.IsNullOrWhiteSpace(changed.CurrencyCode) ? "INR" : changed.CurrencyCode;
                accounts.Add(changed);
                if (apply) await _db.EmployeeBankAccounts.AddAsync(changed);
            }
            else
            {
                changed = accounts.FirstOrDefault(a => a.IdEmployeeBankAccount == recordId)
                          ?? throw new InvalidOperationException("The bank account no longer exists.");
                if (def.Operation == "REMOVE")
                {
                    accounts.Remove(changed);
                    if (apply) _db.EmployeeBankAccounts.Remove(changed);
                    changed = null;
                }
                else
                {
                    if (!apply)
                    {
                        // work on a copy so nothing is tracked
                        changed = new EmployeeBankAccount
                        {
                            IdEmployeeBankAccount = changed.IdEmployeeBankAccount, IdEmployee = changed.IdEmployee, IdBank = changed.IdBank,
                            IdBankBranch = changed.IdBankBranch, AccountNumber = changed.AccountNumber, BranchCode = changed.BranchCode,
                            DisbursementType = changed.DisbursementType, SalaryPercentageDistributed = changed.SalaryPercentageDistributed,
                            CurrencyCode = changed.CurrencyCode, OrderNumber = changed.OrderNumber
                        };
                        accounts[accounts.FindIndex(a => a.IdEmployeeBankAccount == changed.IdEmployeeBankAccount)] = changed;
                    }
                    foreach (var kv in values) SetRaw(changed, kv.Key, kv.Value);
                    changed.BranchCode ??= "";
                }
            }

            if (!string.IsNullOrEmpty(changed?.AccountNumber) &&
                accounts.Any(a => a != changed && a.IdBank == changed!.IdBank && string.Equals(a.AccountNumber, changed.AccountNumber, StringComparison.OrdinalIgnoreCase)))
                throw new InvalidOperationException("This bank account is already in your profile.");

            if (changed != null && changed.SalaryPercentageDistributed == 100)
                foreach (var a in accounts.Where(a => a != changed)) a.SalaryPercentageDistributed = 0;
            else if (accounts.Count == 1)
                accounts[0].SalaryPercentageDistributed = 100;

            var total = accounts.Sum(a => a.SalaryPercentageDistributed);
            if (accounts.Any() && total != 100)
                throw new InvalidOperationException($"Salary % across bank accounts must total 100. After this change it would be {total:0.##}%. Set this account to 100%, or adjust the other accounts first.");
        }

        #endregion

        #region Notifications

        private async Task NotifyCurrentApprovers(ProfileChangeRequests request, int senderId, string typeName, string verb)
        {
            var pending = await GetPendingAllocation(request);
            var approvers = ParseIds(pending?.TargetIdEmployee).Where(id => id != request.IdEmployee).ToList();
            if (!approvers.Any()) return;
            var emp = await _db.Employees.AsNoTracking().Where(e => e.IdEmployee == request.IdEmployee)
                .Select(e => new { e.EmployeeCode, Name = (e.FirstName + " " + (e.LastName ?? "")).Trim() }).FirstOrDefaultAsync();
            await Notify(approvers, senderId, request,
                $"Profile change request {request.RequestNumber} for approval",
                $"{emp?.EmployeeCode} {emp?.Name}: request {request.RequestNumber} ({typeName}) was {verb}.",
                "/dashboard/profile-change-requests");
        }

        // In-app notification + email. Never includes the changed values.
        private async Task Notify(List<int> employeeIds, int senderId, ProfileChangeRequests request, string subject, string message, string link)
        {
            try
            {
                var recipients = await _db.Employees.AsNoTracking()
                    .Where(e => e.IdEmployee.HasValue && employeeIds.Contains(e.IdEmployee.Value))
                    .Select(e => new { Id = e.IdEmployee!.Value, e.EmailID }).ToListAsync();
                foreach (var r in recipients)
                {
                    await _db.Notifications.AddAsync(new Notification
                    {
                        NotificationType = "PROFILECHANGE",
                        SentByIdEmployee = senderId,
                        ReceivedByIdEmployee = r.Id,
                        EmailSubject = Truncate(subject, 200),
                        EmailContent = null,
                        EmailSentStatus = string.IsNullOrWhiteSpace(r.EmailID) ? "NOEMAIL" : "SENT",
                        AppNotificationText = Truncate(message, 250),
                        NotificationLink = link,
                        IsReadAppNotification = false,
                        CreatedAt = DateTime.Now,
                        Status = "SENT",
                        RelatedRecordID = request.IdProfileChangeRequest,
                        RelatedRecordType = request.EntityCode
                    });
                }
                await _db.SaveChangesAsync();

                foreach (var r in recipients.Where(r => !string.IsNullOrWhiteSpace(r.EmailID)))
                {
                    try
                    {
                        await EmailService.SendMail(r.EmailID!, subject,
                            $"<p>{System.Net.WebUtility.HtmlEncode(message)}</p><p>Please log in to NOMARO HRMS for details.</p>");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Profile change email to employee {Id} failed.", r.Id);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Could not create profile change notifications for request {Id}.", request.IdProfileChangeRequest);
            }
        }

        #endregion

        #region Settings

        public async Task UpdateRequestType(ProfileChangeRequestTypeUpdateDto dto, int userId)
        {
            var row = await _db.ProfileChangeRequestTypes.FirstOrDefaultAsync(t => t.IdProfileChangeRequestType == dto.IdProfileChangeRequestType)
                      ?? throw new KeyNotFoundException("Request type not found.");
            if (string.IsNullOrWhiteSpace(dto.RequestTypeName) || dto.RequestTypeName.Length > 100)
                throw new InvalidOperationException("Name is required (up to 100 characters).");
            if ((dto.ProofHint?.Length ?? 0) > 250) throw new InvalidOperationException("Proof hint must not exceed 250 characters.");

            var before = new { row.RequestTypeName, row.ProofRequired, row.ProofHint, row.DisplayOrder, row.IsActive };
            row.RequestTypeName = dto.RequestTypeName.Trim();
            row.ProofRequired = dto.ProofRequired;
            row.ProofHint = string.IsNullOrWhiteSpace(dto.ProofHint) ? null : dto.ProofHint.Trim();
            row.DisplayOrder = dto.DisplayOrder;
            row.IsActive = dto.IsActive;
            row.ModifiedBy = userId;
            row.ModifiedDateTime = DateTime.Now;
            await _db.SaveChangesAsync();
            await _audit.LogAuditAsync("Update", "ProfileChangeRequestType", row.IdProfileChangeRequestType,
                new { before, after = new { row.RequestTypeName, row.ProofRequired, row.ProofHint, row.DisplayOrder, row.IsActive } });
        }

        private static string WorkflowName(string code) => code switch
        {
            "PROFCHGID" => "Profile Change - Identity & Statutory",
            "PROFCHGBANK" => "Profile Change - Bank",
            _ => "Profile Change - Personal, Contact, Family & Documents"
        };

        public async Task<List<ProfileChangeWorkflowDto>> GetWorkflows()
        {
            var result = new List<ProfileChangeWorkflowDto>();
            foreach (var code in EntityCodes)
            {
                var config = await _db.WorkFlowConfig.AsNoTracking().FirstOrDefaultAsync(c => c.EntityCode == code);
                var dto = new ProfileChangeWorkflowDto { EntityCode = code, EntityName = config?.EntityName ?? WorkflowName(code) };
                if (config != null)
                {
                    var levels = await _db.WorkFlowConfigDetails.AsNoTracking()
                        .Where(d => d.IdWorkFlowConfig == config.IdWorkFlowConfig).OrderBy(d => d.LevelNumber).ToListAsync();
                    foreach (var l in levels)
                    {
                        string? name = l.ApprovalAuthorityType switch
                        {
                            "ROLE" => await _db.Designations.AsNoTracking().Where(d => d.IdDesignation == l.ApprovalAuthorityID).Select(d => d.DesignationName).FirstOrDefaultAsync(),
                            "EMPLOYEE" => await _db.Employees.AsNoTracking().Where(e => e.IdEmployee == l.ApprovalAuthorityID).Select(e => (e.EmployeeCode + " " + e.FirstName + " " + (e.LastName ?? "")).Trim()).FirstOrDefaultAsync(),
                            "REPOFFICER" => "Reporting officer",
                            _ => null
                        };
                        dto.Levels.Add(new ProfileChangeWorkflowLevelDto
                        {
                            LevelNumber = l.LevelNumber,
                            ApprovalAuthorityType = l.ApprovalAuthorityType,
                            ApprovalAuthorityID = l.ApprovalAuthorityID,
                            AuthorityName = name
                        });
                    }
                }
                result.Add(dto);
            }
            return result;
        }

        public async Task SaveWorkflow(ProfileChangeWorkflowDto dto)
        {
            if (!EntityCodes.Contains(dto.EntityCode)) throw new InvalidOperationException("Unknown workflow.");
            var levels = (dto.Levels ?? new()).OrderBy(l => l.LevelNumber).ToList();
            if (levels.Count < 1 || levels.Count > 5) throw new InvalidOperationException("Set between 1 and 5 approval levels.");

            for (var i = 0; i < levels.Count; i++)
            {
                var l = levels[i];
                l.LevelNumber = i + 1;
                switch (l.ApprovalAuthorityType)
                {
                    case "ROLE":
                        if (l.ApprovalAuthorityID is not > 0 || !await _db.Designations.AnyAsync(d => d.IdDesignation == l.ApprovalAuthorityID))
                            throw new InvalidOperationException($"Level {l.LevelNumber}: select a designation.");
                        break;
                    case "EMPLOYEE":
                        if (l.ApprovalAuthorityID is not > 0 || !await _db.Employees.AnyAsync(e => e.IdEmployee == l.ApprovalAuthorityID))
                            throw new InvalidOperationException($"Level {l.LevelNumber}: select an employee.");
                        break;
                    case "REPOFFICER":
                        l.ApprovalAuthorityID = 0;   // the engine needs a value here
                        break;
                    default:
                        throw new InvalidOperationException($"Level {l.LevelNumber}: select who approves.");
                }
            }

            var open = await _db.ProfileChangeRequests.AnyAsync(r => r.EntityCode == dto.EntityCode && R.WithApproverStatuses.Contains(r.RequestStatus) && r.CurrentLevel > levels.Count);
            if (open) throw new InvalidOperationException("Some requests are already past the new last level. Finish them before reducing the levels.");

            await using var tx = await _db.Database.BeginTransactionAsync();
            var config = await _db.WorkFlowConfig.FirstOrDefaultAsync(c => c.EntityCode == dto.EntityCode);
            if (config == null)
            {
                config = new WorkFlowConfig
                {
                    EntityCode = dto.EntityCode,
                    EntityName = WorkflowName(dto.EntityCode),
                    ApprovalCycleCount = levels.Count,
                    MainTableName = "ProfileChangeRequests",
                    MainColumnName = "IdEmployee"
                };
                await _db.WorkFlowConfig.AddAsync(config);
                await _db.SaveChangesAsync();
            }
            else
            {
                config.ApprovalCycleCount = levels.Count;
            }

            var existing = await _db.WorkFlowConfigDetails.Where(d => d.IdWorkFlowConfig == config.IdWorkFlowConfig).ToListAsync();
            _db.WorkFlowConfigDetails.RemoveRange(existing);
            await _db.WorkFlowConfigDetails.AddRangeAsync(levels.Select(l => new WorkFlowConfigDetails
            {
                IdWorkFlowConfig = config.IdWorkFlowConfig,
                LevelNumber = l.LevelNumber,
                ApprovalAuthorityType = l.ApprovalAuthorityType,
                ApprovalAuthorityID = l.ApprovalAuthorityID,
                ApprovalStatusName = l.LevelNumber == levels.Count ? "FINAL APPROVED" : "INTERIM APPROVED"
            }));
            await _db.SaveChangesAsync();
            await tx.CommitAsync();

            await _audit.LogAuditAsync("Update", "ProfileChangeWorkflow", config.IdWorkFlowConfig,
                new { dto.EntityCode, Levels = levels.Select(l => new { l.LevelNumber, l.ApprovalAuthorityType, l.ApprovalAuthorityID }) });
        }

        public async Task<ProfileChangeWorkflowLookupsDto> GetWorkflowLookups() => new()
        {
            Designations = await _db.Designations.AsNoTracking().OrderBy(d => d.DesignationName)
                .Select(d => new LookupOptionDto { Value = d.IdDesignation.ToString(), Label = d.DesignationName }).ToListAsync(),
            Employees = await _db.Employees.AsNoTracking().Where(e => e.CurrentStatus == "Working")
                .OrderBy(e => e.FirstName)
                .Select(e => new LookupOptionDto { Value = e.IdEmployee!.Value.ToString(), Label = (e.EmployeeCode + " - " + e.FirstName + " " + (e.LastName ?? "")).Trim() })
                .ToListAsync()
        };

        #endregion
    }
}
