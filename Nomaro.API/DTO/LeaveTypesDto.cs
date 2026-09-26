
using System.ComponentModel.DataAnnotations;
namespace Nomaro.API.DTO
{
    public class LeaveTypesDto
    {
        [Key]
        public int IdLeaveType { get; set; }
        public string LeaveCode { get; set; }
        public string LeaveTypeName { get; set; }
    }
}

