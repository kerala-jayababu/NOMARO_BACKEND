namespace Nomaro.API.DTO
{
    public class RentFreeQuarterAllowanceAddOrUpdateDto
    {
        public int? IdRentFreeQuarterAllowance { get; set; }  // nullable for Add case
        public int IdEmployee { get; set; }
        public int Duration { get; set; }
        public int FinancialYear { get; set; }
        public int AllottedSqFt { get; set; }
        public decimal SqFtRate { get; set; }
        public decimal? AnnualRFQAllowance { get; set; }
        public decimal? TaxFreeAllowance { get; set; }
        public decimal? TaxableAmount { get; set; }
        public decimal? TaxAmount { get; set; }
        public decimal? TaxRate {  get; set; }
        public decimal? NetRFQAllowance { get; set; }
    }
}

