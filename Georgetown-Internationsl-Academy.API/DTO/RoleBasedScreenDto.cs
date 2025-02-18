using Georgetown_Internationsl_Academy.API.Models;
using System.ComponentModel.DataAnnotations.Schema;

namespace Georgetown_Internationsl_Academy.API.DTO
{
    public class RoleBasedScreenDto
    {
        public int? IdPayrollScreen { get; set; }
        public string ScreenName { get; set; }
        public string? ValidPermissions { get; set; }
        public int? IdParentPayrollScreen { get; set; }

        [NotMapped]
        public int? IdRolePermission { get; set; }       


        public List<PayrollScreens>? SubMenus { get; set; }
    }
}
