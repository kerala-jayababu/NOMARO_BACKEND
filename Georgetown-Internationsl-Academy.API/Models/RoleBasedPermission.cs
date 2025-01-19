using System.ComponentModel.DataAnnotations;

namespace Georgetown_Internationsl_Academy.API.Models
{
    public class RoleBasedPermission
    {
        [Key]
        public int IdRolePermission { get; set; }
        public int IdDesignation { get; set; }
        public int IdPayrollScreen { get; set; }
        public string Permission { get; set; }
    }
}
