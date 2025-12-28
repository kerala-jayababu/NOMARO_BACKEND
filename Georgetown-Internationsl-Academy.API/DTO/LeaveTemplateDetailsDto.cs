
using System.ComponentModel.DataAnnotations;
namespace Georgetown_Internationsl_Academy.API.DTO
{
    public class LeaveTemplateDetailsDto
    {
        [Key]
        public int IdLeaveTemplateDetails { get; set; }
        public int IdLeaveTemplate { get; set; }
        public int IdLeaveType { get; set; }
        public int IdAnnualLeaveTypeConfig { get; set; }
        public int NoOfDaysInYear { get; set; }
    }
}
