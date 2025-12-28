
using System.ComponentModel.DataAnnotations;
namespace Georgetown_Internationsl_Academy.API.DTO
{
    public class EmployeeLeaveConfigDetailsDto
    {
        [Key]
        public int IdEmployeeLeaveConfigDetail { get; set; }
        public int IdEmployeeLeaveConfig { get; set; }
        public int IdLeaveType { get; set; }
        public int AllocatedDays { get; set; }
        public int UsedLeaveDays { get; set; }
        public int BalanceLeaveDays { get; set; }
    }
}
