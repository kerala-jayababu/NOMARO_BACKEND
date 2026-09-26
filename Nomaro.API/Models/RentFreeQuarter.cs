using System.ComponentModel.DataAnnotations;

namespace Nomaro.API.Models
{
    public class RentFreeQuarter
    {
        [Key]
        public int? IdRentFreeQuater { get; set; }
        public int IdRentFreeQuarterEnum { get; set; }
        public int IdEmployee { get; set; }
        public string PeriodText { get; set; }
        public decimal TotalAnnualRent { get; set; }
        public int DurationInMonths { get; set; }
        public DateTime ValidFrom { get; set; }
        public DateTime? ValidTo { get; set; }
        public decimal MonthlyRent { get; set; }
        public decimal TaxRate { get; set; }
        public decimal AnnualTaxAmount { get; set; }
        public decimal MonthlyTaxAmount { get; set; }
    }
}

