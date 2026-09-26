using System.ComponentModel.DataAnnotations;

namespace Nomaro.API.DTO
{
    public class NoticePeriodPolicyDto
    {
        [Key]
        public int IdNoticePeriodPolicy { get; set; }
        public string PolicyCode { get; set; }
        public string PolicyName { get; set; }
        public string AppliesToEmployeeType { get; set; }
        public int NoticeDays { get; set; }
        public bool IsActive { get; set; }
    }
}
