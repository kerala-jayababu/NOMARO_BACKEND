namespace Nomaro.API.DTO
{
    public class SalaryGenerationStatusDto
    {
        public int IdEmployee { get; set; }

        public string EmployeeCode { get; set; } = string.Empty;
        public string EmployeeName { get; set; }
        public  string DesignationName { get; set; }
        public string? SalaryGenerationStatus { get; set; } = string.Empty;
    
    }
}

