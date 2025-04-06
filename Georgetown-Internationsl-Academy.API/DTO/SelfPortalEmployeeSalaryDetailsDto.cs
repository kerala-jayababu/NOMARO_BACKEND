namespace Georgetown_Internationsl_Academy.API.DTO
{
    public class SelfPortalEmployeeSalaryDetailsDto
    {
        public int IdEmployeeSalaryDetail { get; set; }
        public string SalaryHeadName { get; set; }
        public string HeadType { get; set; }
        public decimal AmountGYD { get; set; }
        public decimal AmountUSD { get; set; }
        public decimal YTDAmount { get; set; }
    }
}
