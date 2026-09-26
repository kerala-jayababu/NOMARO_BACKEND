namespace Nomaro.API.Models
{
    using System;
    using System.ComponentModel.DataAnnotations;

    namespace YourNamespace.Models
    {
        public class RentFreeQuarterAllowance
        {
            [Key]
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
        }
    }

}

