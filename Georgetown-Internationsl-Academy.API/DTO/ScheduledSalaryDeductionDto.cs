namespace Georgetown_Internationsl_Academy.API.DTO
{
    public class ScheduledSalaryDeductionDto
    {
        public int? IdScheduledSalaryDeduction { get; set; }
        public int IdEmployee { get; set; }
        public decimal TotalAmount { get; set; }
        public int DeductionFromSalaryMonth { get; set; }
        public int DeductionToSalaryMonth { get; set; }
        public int AllocatingSalaryHead { get; set; }
        public int MonthCount { get; set; }
        public decimal MonthlyDeductableAmount { get; set; }
    }
}
