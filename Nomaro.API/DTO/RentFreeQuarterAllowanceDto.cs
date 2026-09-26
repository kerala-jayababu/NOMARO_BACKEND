namespace Nomaro.API.DTO
{
    public class RentFreeQuarterAllowanceDto
    {
        public int IdRentFreeQuarterAllowance { get; set; }
        public int IdEmployee { get; set; }
        public int Duration { get; set; }
        public int FinancialYear { get; set; }
        public int AllottedSqft { get; set; }
        public decimal SqFtRate { get; set; }
        public decimal? AnnualRFQAllowance { get; set; }
        public decimal? TaxFreeAllowance { get; set; }
        public decimal? TaxableAmount { get; set; }
        public decimal? TaxAmount { get; set; }
        public decimal? TaxRate { get; set; }
        public decimal? NetRFQAllowance { get; set; }
        public DateTime? CreatedDate { get; set; }
        public DateTime? UpdatedDate { get; set; }
        public int? CreatedBy { get; set; }

        // Optional: joined fields for display
        public string? EmployeeCode { get; set; }
        public string? EmployeeName { get; set; }
        public string? DepartmentName { get; set; }
        public string? DesignationName { get; set; }
        public string? FinancialYearName { get; set; }
    }
}

