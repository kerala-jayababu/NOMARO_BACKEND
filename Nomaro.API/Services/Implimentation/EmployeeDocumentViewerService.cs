using Microsoft.EntityFrameworkCore;
using Nomaro.API.Database;
using Nomaro.API.DTO;
using Nomaro.API.Services.Interface;

namespace Nomaro.API.Services.Implimentation
{
    /// <summary>
    /// HR view of documents uploaded against employee profiles (EmployeeDocuments).
    /// </summary>
    public class EmployeeDocumentViewerService : IEmployeeDocumentViewerService
    {
        private readonly ApplicationDBContext _db;
        private readonly IAuditService _audit;
        private readonly ILogger<EmployeeDocumentViewerService> _logger;

        public EmployeeDocumentViewerService(ApplicationDBContext db, IAuditService audit, ILogger<EmployeeDocumentViewerService> logger)
        {
            _db = db;
            _audit = audit;
            _logger = logger;
        }

        public async Task<List<EmployeeDocumentListDto>> GetDocuments(EmployeeDocumentFilterDto filter)
        {
            var query =
                from d in _db.EmployeeDocuments.AsNoTracking()
                join e in _db.Employees.AsNoTracking() on (int?)d.IdEmployee equals e.IdEmployee
                join t in _db.DocumentTypes.AsNoTracking() on d.IdDocumentType equals t.IdDocumentType into ts
                from t in ts.DefaultIfEmpty()
                join dep in _db.Departments.AsNoTracking() on e.IdDepartment equals (int?)dep.IdDepartment into deps
                from dep in deps.DefaultIfEmpty()
                join des in _db.Designations.AsNoTracking() on e.IdDesignation equals (int?)des.IdDesignation into dess
                from des in dess.DefaultIfEmpty()
                select new { d, e, TypeName = t != null ? t.DocumentTypeName : null, Dept = dep != null ? dep.DepartmentName : null, Desig = des != null ? des.DesignationName : null };

            if (filter.IdDocumentType is > 0)
                query = query.Where(x => x.d.IdDocumentType == filter.IdDocumentType);

            if (!string.IsNullOrWhiteSpace(filter.EmployeeStatus))
                query = query.Where(x => x.e.CurrentStatus == filter.EmployeeStatus);

            var today = DateTime.Today;
            switch (filter.Validity?.ToUpperInvariant())
            {
                case "EXPIRED":
                    query = query.Where(x => x.d.DocumentValidTill != null && x.d.DocumentValidTill < today);
                    break;
                case "EXPIRING":
                    var limit = today.AddDays(30);
                    query = query.Where(x => x.d.DocumentValidTill != null && x.d.DocumentValidTill >= today && x.d.DocumentValidTill <= limit);
                    break;
                case "VALID":
                    query = query.Where(x => x.d.DocumentValidTill == null || x.d.DocumentValidTill >= today);
                    break;
            }

            if (!string.IsNullOrWhiteSpace(filter.Search))
            {
                // every word must match the code or a part of the name: "anitha ram" finds Anitha Ramesh
                foreach (var word in filter.Search.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries).Take(5))
                {
                    var w = word;
                    query = query.Where(x =>
                        (x.e.EmployeeCode != null && x.e.EmployeeCode.Contains(w)) ||
                        (x.e.FirstName != null && x.e.FirstName.Contains(w)) ||
                        (x.e.MiddleName != null && x.e.MiddleName.Contains(w)) ||
                        (x.e.LastName != null && x.e.LastName.Contains(w)));
                }
            }

            var rows = await query
                .OrderBy(x => x.e.FirstName).ThenBy(x => x.e.LastName).ThenByDescending(x => x.d.CreatedAt)
                .Take(2000)
                .ToListAsync();

            return rows.Select(x => new EmployeeDocumentListDto
            {
                IdEmployeeDocument = x.d.IdEmployeeDocument,
                IdEmployee = x.d.IdEmployee,
                EmployeeCode = x.e.EmployeeCode,
                EmployeeName = string.Join(" ", new[] { x.e.FirstName, x.e.MiddleName, x.e.LastName }.Where(n => !string.IsNullOrWhiteSpace(n))),
                DepartmentName = x.Dept,
                DesignationName = x.Desig,
                CurrentStatus = x.e.CurrentStatus,
                IdDocumentType = x.d.IdDocumentType,
                DocumentTypeName = x.TypeName,
                Remarks = x.d.Remarks,
                DocumentValidTill = x.d.DocumentValidTill,
                CreatedAt = x.d.CreatedAt,
                UpdatedAt = x.d.UpdatedAt,
                FileName = OriginalFileName(x.d.DocumentFilePath),
                HasFile = !string.IsNullOrWhiteSpace(x.d.DocumentFilePath)
            }).ToList();
        }

        public async Task<FileDownloadDto?> GetDocumentFile(int idEmployeeDocument)
        {
            var doc = await _db.EmployeeDocuments.AsNoTracking().FirstOrDefaultAsync(d => d.IdEmployeeDocument == idEmployeeDocument);
            if (doc == null || string.IsNullOrWhiteSpace(doc.DocumentFilePath)) return null;

            var path = ResolvePath(doc.DocumentFilePath);
            if (path == null)
            {
                _logger.LogWarning("Employee document {Id}: file not found at {Path}", idEmployeeDocument, doc.DocumentFilePath);
                return null;
            }

            var name = OriginalFileName(doc.DocumentFilePath) ?? Path.GetFileName(path);
            await _audit.LogAuditAsync("View", "EmployeeDocument", idEmployeeDocument, new { doc.IdEmployee, doc.IdDocumentType, FileName = name });

            return new FileDownloadDto
            {
                Content = await File.ReadAllBytesAsync(path),
                ContentType = Path.GetExtension(name).ToLowerInvariant() switch
                {
                    ".pdf" => "application/pdf",
                    ".png" => "image/png",
                    ".jpg" or ".jpeg" => "image/jpeg",
                    ".gif" => "image/gif",
                    ".doc" => "application/msword",
                    ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                    ".xls" => "application/vnd.ms-excel",
                    ".xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    _ => "application/octet-stream"
                },
                FileName = name
            };
        }

        // The stored path is absolute and may come from another server or folder (e.g. C:\Payroll\NEXGEN\...).
        // Fall back to the same file name in this API's employeedocuments folder.
        private static string? ResolvePath(string stored)
        {
            if (File.Exists(stored)) return stored;
            var fileName = Path.GetFileName(stored.Replace('\\', Path.DirectorySeparatorChar).Replace('/', Path.DirectorySeparatorChar));
            if (string.IsNullOrWhiteSpace(fileName)) return null;
            var local = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "employeedocuments", fileName);
            return File.Exists(local) ? local : null;
        }

        // "3144f54b-..._NIS.pdf" -> "NIS.pdf"
        private static string? OriginalFileName(string? path)
        {
            if (string.IsNullOrWhiteSpace(path)) return null;
            var name = path.Replace('\\', '/');
            name = name[(name.LastIndexOf('/') + 1)..];
            var underscore = name.IndexOf('_');
            if (underscore > 0 && Guid.TryParse(name[..underscore], out _)) name = name[(underscore + 1)..];
            return name;
        }
    }
}
