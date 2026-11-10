namespace Nomaro.API.DTO
{
    public class EmployeeDocumentListDto
    {
        public int IdEmployeeDocument { get; set; }
        public int IdEmployee { get; set; }
        public string? EmployeeCode { get; set; }
        public string? EmployeeName { get; set; }
        public string? DepartmentName { get; set; }
        public string? DesignationName { get; set; }
        public string? CurrentStatus { get; set; }
        public int IdDocumentType { get; set; }
        public string? DocumentTypeName { get; set; }
        public string? Remarks { get; set; }
        public DateTime? DocumentValidTill { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public string? FileName { get; set; }
        public bool HasFile { get; set; }
    }

    public class EmployeeDocumentFilterDto
    {
        public int? IdDocumentType { get; set; }     // null or 0 = all types
        public string? Search { get; set; }          // employee name or code
        public string? EmployeeStatus { get; set; }  // Working | NotWorking | null = all
        public string? Validity { get; set; }        // VALID | EXPIRED | EXPIRING (30 days) | null = all
    }
}
