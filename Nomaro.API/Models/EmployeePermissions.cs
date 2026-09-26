using System.ComponentModel.DataAnnotations;

namespace Nomaro.API.Models
{
    public class EmployeePermissions
    {
        [Key]
        public int IdEmployeePermission { get; set; }
        public int IdEmployee { get; set; }
        public int IdPayrollScreen { get; set; }
        public string Permission { get; set; }
    }
}

