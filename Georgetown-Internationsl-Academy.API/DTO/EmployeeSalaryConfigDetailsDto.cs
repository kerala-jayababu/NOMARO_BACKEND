namespace Georgetown_Internationsl_Academy.API.DTO
{
    public class EmployeeSalaryConfigDetailsDto
    {
        public int? IdEmployeeSalaryConfigDetail { get; set; }
        public int IdEmployeeSalaryConfig { get; set; }
        public int IdSalaryHead { get; set; }
        public string CalculationMethod { get; set; }
        public decimal? FixedAmount { get; set; }
        public decimal? PercentageValue { get; set; }
        public string? CustomFormula { get; set; }
        public decimal? SalaryAmount { get; set; }
    }
}
