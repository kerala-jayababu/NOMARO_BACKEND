namespace Georgetown_Internationsl_Academy.API.DTO
{
    public class SalaryMonthsDto
    {
        public int IdSalaryMonth { get; set; }
        public string SalaryMonthText { get; set; } = string.Empty;
        public DateTime SalaryMonthDate { get; set; }
        public string? SalaryGenerationStatus { get; set; }
    }
}
