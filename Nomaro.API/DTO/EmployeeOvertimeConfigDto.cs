namespace Nomaro.API.DTO
{
    public class EmployeeOvertimeConfigDto
    {
        public int IdEmployeeOvertimeConfig { get; set; }
        public int IdEmployee { get; set; }
        public string DayType { get; set; }
        public decimal StandardRate { get; set; } 
        public decimal? DayRate { get; set; }
        public int? IdSalaryMonthAdjusedt { get; set; }
        public decimal? AmountAdjusted { get; set; }
        public string? SalarMonth { get; set; }
    }
}

