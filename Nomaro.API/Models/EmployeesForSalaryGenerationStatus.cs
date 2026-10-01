using Microsoft.EntityFrameworkCore;

namespace Nomaro.API.Models
{
    /// <summary>
    /// Employees selected for the current salary generation session and the result per employee
    /// (SUCCESS or the error message written by GenerateMonthlySalary).
    /// </summary>
    [PrimaryKey(nameof(IdEmployee), nameof(IdSalaryMonth))]
    public class EmployeesForSalaryGenerationStatus
    {
        public int IdEmployee { get; set; }
        public int IdSalaryMonth { get; set; }
        public string? SalaryGenerationRemarks { get; set; }
    }
}
