namespace Nomaro.API.DTO
{
    public class ShiftManagerEmployeeDto
    {
        public int IdEmployee { get; set; }
        public string EmployeeName { get; set; } = string.Empty;
        public string? Contact { get; set; }
    }
}