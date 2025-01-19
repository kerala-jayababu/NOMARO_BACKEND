using System.ComponentModel.DataAnnotations;

namespace Georgetown_Internationsl_Academy.API.Models
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
