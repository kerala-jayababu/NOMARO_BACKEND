using System.ComponentModel.DataAnnotations.Schema;

namespace Nomaro.API.DTO
{
  
    public class EmployeeBankAccountDto
    {
        public int IdEmployeeBankAccount { get; set; }
        public int IdEmployee { get; set; }
        public int IdBank { get; set; }
        public string AccountNumber { get; set; }
        public int?  IdBankBranch { get; set; }         
        public string BranchCode { get; set; }
        public decimal SalaryPercentageDistributed { get; set; }
        public string CurrencyCode { get; set; }
        public string? ABARoutingNumber { get; set; }
        public string? DisbursementType {  get; set; }

        [NotMapped]
        public string? BankName { get; set; }
        [NotMapped]
        public string? BranchName { get; set; }
        public int? OrderNumber { get; set; }

    }

}

