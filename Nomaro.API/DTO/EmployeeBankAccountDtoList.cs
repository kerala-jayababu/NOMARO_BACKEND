namespace Nomaro.API.DTO
{
    public class EmployeeBankAccountDtoList
    {
        public int? IdEmployeeBankAccount { get; set; }
        public int IdEmployee { get; set; }
        public int IdBank { get; set; }
        public int IdBankBranch { get; set; }
        public string AccountNumber { get; set; }
        public string DisbursementType { get; set; }
        public string? BranchCode { get; set; }
        public decimal SalaryPercentageDistributed { get; set; }
        public string CurrencyCode { get; set; }
        public int? OrderNumber { get; set; }
    }
}

