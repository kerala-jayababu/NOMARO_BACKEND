using System.ComponentModel.DataAnnotations;

namespace Georgetown_Internationsl_Academy.API.Models
{
    public class EmployeeBankAccount
    {
        [Key]
        public int IdEmployeeBankAccount { get; set; }
        public int IdEmployee { get; set; }
        public int IdBank { get; set; }
        public int? IdBankBranch { get; set; }
        public string AccountNumber { get; set; }        
        public string BranchCode { get; set; }
        public decimal SalaryPercentageDistributed { get; set; }
        public string CurrencyCode { get; set; }
    }
}
