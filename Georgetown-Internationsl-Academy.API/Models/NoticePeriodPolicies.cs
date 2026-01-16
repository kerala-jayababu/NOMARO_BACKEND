using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Georgetown_Internationsl_Academy.API.Models
{
    public class NoticePeriodPolicies
    {
        [Key]
        public int IdNoticePeriodPolicy { get; set; }
        public string PolicyCode { get; set; }
        public string PolicyName { get; set; }
        public string AppliesToEmployeeType { get; set; }
        public int NoticeDays { get; set; }
        public bool IsActive { get; set; }

        public int CreatedBy { get; set; }
        public DateTime CreatedAt { get; set; }

        public int? UpdatedBy { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}