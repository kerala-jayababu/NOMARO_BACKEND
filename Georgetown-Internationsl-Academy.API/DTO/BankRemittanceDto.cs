namespace Georgetown_Internationsl_Academy.API.DTO
{
    public class BankRemittanceDto
    {
        public int IdBankRemittance { get; set; }
        public string? BankName { get; set; }
        public string? AccountNumber { get; set; }
        public string? ABARoutingNumber { get; set; }
        public decimal? AmountGTD { get; set; }
        public decimal? AmountUSD { get; set; }
        public decimal? DistributedPercent { get; set; }
        public string? Currency { get; set; }
    }
}
