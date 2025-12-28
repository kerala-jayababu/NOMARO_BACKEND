
using System.ComponentModel.DataAnnotations;
namespace Georgetown_Internationsl_Academy.API.DTO
{
    public class LeaveTypes
    {
        [Key]
        public int IdLeaveType { get; set; }
        public string LeaveCode { get; set; }
        public string LeaveTypeName { get; set; }
    }
}
