using System.ComponentModel.DataAnnotations;

namespace Nomaro.API.DTO
{
    public class EmployeeNameList
    {
        [Key]
        public int Idemployee { get; set; }
        public string EmployeeCode { get; set; }
        public string EmployeeName { get; set; }
    }
}

