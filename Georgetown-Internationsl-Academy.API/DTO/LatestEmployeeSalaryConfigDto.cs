namespace Georgetown_Internationsl_Academy.API.DTO
{
    public class LatestEmployeeSalaryConfigDto
    {
        public int IdEmployeeSalaryConfig { get; set; }
        public int IdEmployee { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string? MiddleName { get; set; } // Nullable
        public string LastName { get; set; } = string.Empty;
        public DateTime ValidFrom { get; set; }
        public DateTime? ValidTo { get; set; } // Nullable
        public int? IdSalaryTemplate { get; set; } // Nullable
        public int CreatedBy { get; set; }
        public DateTime CreatedOn { get; set; }
        public string ApprovalStatus { get; set; } = string.Empty;
        public bool ActiveStatus { get; set; }
        public decimal TotalEarnings { get; set; }
        public decimal TotalDeductions { get; set; }
        public decimal NetSalary { get; set; }
    }
}
