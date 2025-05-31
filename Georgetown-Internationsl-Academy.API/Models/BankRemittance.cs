using System.ComponentModel.DataAnnotations;

namespace Georgetown_Internationsl_Academy.API.Models
{
    public class BankRemittance
    {
        [Key]
        public int IdBankRemittance { get; set; }
        public int IdEmployee { get; set; }
        public int? IdEmployeeSalary { get; set; }
        public int? IdSalaryMonth { get; set; }
        public int? IdBank { get; set; }
        public int? IdBankBranch { get; set; }
        public string ABARoutingNumber { get; set; }
        public string? BankName { get; set; }
        public string? AccountNumber { get; set; }
        public decimal? AmountGYD { get; set; }
        public decimal? AmountUSD { get; set; }
        public decimal? DistributedPercentage { get; set; }
        public string Currency { get; set; }
    }
}
