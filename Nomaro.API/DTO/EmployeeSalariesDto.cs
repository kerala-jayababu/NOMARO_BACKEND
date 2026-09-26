namespace Nomaro.API.DTO
{
    public class EmployeeSalariesDto
    {
        public int IdEmployeeSalary{get; set; }
        public int? IdSalaryMonth { get; set; }
        public string 	SalaryMonthName{ get; set; }
        public decimal TotalEarnings { get; set; }
        public decimal 	TotalDeductions{ get; set; }
  }
}
    
