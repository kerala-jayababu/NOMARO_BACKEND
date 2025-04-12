using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Georgetown_Internationsl_Academy.API.Models
{
    public class PayrollScreens
    {
        [Key]
        public int IdPayrollScreen { get; set; }
        public string ScreenName { get; set; }
        public string? ScreenCode { get; set; }
        public string? ValidPermissions { get; set; }
        public int? IdParentPayrollScreen { get; set; }
        public int? OrderNumber { get; set; }
        public string APPTYPE { get; set; }
        public bool? Enabled { get; set; }

        [NotMapped]
        public int? IdEmployeePermission { get; set; }

        [NotMapped]
        public int? IdRolePermission { get; set; }  
    }
}
