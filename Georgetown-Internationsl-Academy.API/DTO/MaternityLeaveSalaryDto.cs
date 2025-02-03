namespace Georgetown_Internationsl_Academy.API.DTO
{
    public class MaternityLeaveSalaryDto
    {
        public int? IdMaternityLeaveSalary { get; set; }
        public int IdEmployee { get; set; }
        public DateTime MaternityLeaveFrom { get; set; }
        public DateTime MaternityLeaveTo { get; set; }
        public int IdSalaryMonthFrom { get; set; }
        public int IdSalaryMonthTo { get; set; }
        public decimal NetSalary { get; set; }
        public decimal MaternityLeaveSalary { get; set; }
    }
}
