using Nomaro.API.Models;
using System.ComponentModel.DataAnnotations.Schema;

namespace Nomaro.API.DTO
{
    public class PayrollScreenDto
    {
        public int? IdPayrollScreen { get; set; } 
        public string ScreenName { get; set; }
        public string? ValidPermissions { get; set; }
        public int? IdParentPayrollScreen { get; set; }

        [NotMapped]
        public int ? IdEmployeePermission { get; set; }
        [NotMapped]
        public int? OrderNumber { get; set; }
        public List<PayrollScreens>? SubMenus { get; set; }
    }
}

