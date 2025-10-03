namespace Georgetown_Internationsl_Academy.API.DTO
{
    public class RentFreeQuarterAllowanceDto
    {
        public int IdEmployee { get; set; }
        public string EmployeeCode { get; set; }
        public string EmployeeName { get; set; }
        public string DepartmentName { get; set; }
        public string DesignationName { get; set; }

        // RFQ Fields
        public int? IdRFQ { get; set; }
        public int? IdFinancialYear { get; set; }
        public decimal? Duration { get; set; }
        public decimal? Sqft { get; set; }
        public decimal? Rate { get; set; }

        // Computed Fields
        public decimal AnnualRFQ { get; set; }        // Duration * Sqft * Rate
        public decimal TaxFree { get; set; }          // AnnualRFQ / 3
        public decimal TaxableAmount { get; set; }    // AnnualRFQ - TaxFree
        public decimal TaxAmount { get; set; }        // 40% of TaxableAmount
        public decimal NetRent { get; set; }          // AnnualRFQ - TaxAmount

        // Financial Year
        public string FinancialYearName { get; set; }
        public DateTime? FinancialYearFrom { get; set; }
        public DateTime? FinancialYearTo { get; set; }
    }
}
