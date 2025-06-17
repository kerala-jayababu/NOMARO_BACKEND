namespace Georgetown_Internationsl_Academy.API.Models
{
    public class EmployeeTaxDetail
    {
        public string TINNumber { get; set; }
        public string EmployeeName { get; set; }
        public decimal? TotalIncome { get; set; }
        public decimal? Deduction { get; set; }
        public decimal? NIS { get; set; }
        public decimal? MLIE { get; set; }
        public decimal? IncomeTax { get; set; }
    }
}
