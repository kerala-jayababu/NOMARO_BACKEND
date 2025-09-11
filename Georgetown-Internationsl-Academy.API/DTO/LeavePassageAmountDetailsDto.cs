namespace Georgetown_Internationsl_Academy.API.DTO
{
    public class LeavePassageAmountDetailsDto
    {
        public int IdLeavePassageAmount { get; set; }   // 0 = new, >0 = update
        public int IdEmployee { get; set; }
        public decimal Amount { get; set; }
        public int IdFinancialYear { get; set; }        // will be set in controller
        public DateTime FinancialYearFrom { get; set; } // optional for insert
        public DateTime FinancialYearTo { get; set; }   // optional for insert
    }

}
