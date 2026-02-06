using System.ComponentModel.DataAnnotations;

namespace Georgetown_Internationsl_Academy.API.DTO
{
    public class EmployeeNameList
    {
        [Key]
        public int Idemployee { get; set; }
        public string EmployeeCode { get; set; }
        public string EmployeeName { get; set; }
    }
}
