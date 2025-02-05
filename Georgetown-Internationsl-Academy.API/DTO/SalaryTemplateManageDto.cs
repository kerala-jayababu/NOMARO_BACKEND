namespace Georgetown_Internationsl_Academy.API.DTO
{
    public class SalaryTemplateManageDto
    {
        public string SalaryTemplateName { get; set; }
        public string? Description { get; set; }
        public bool ActiveStatus { get; set; }
        public decimal? TotalEarnings { get; set; }
        public decimal? TotalDeductions { get; set; }
        public decimal? NetSalary { get; set; }     
    }
}
