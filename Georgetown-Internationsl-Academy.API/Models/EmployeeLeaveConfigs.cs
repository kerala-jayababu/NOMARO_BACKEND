
using System;
using System.ComponentModel.DataAnnotations;
namespace Georgetown_Internationsl_Academy.API.DTO
{
    public class EmployeeLeaveConfigs
    {
        [Key]
        public int IdEmployeeLeaveConfig { get; set; }
        public int IdEmployee { get; set; }
        public int IdLeaveTemplate { get; set; }
        public DateTime EffectiveFrom { get; set; }
        public DateTime? EffectiveTo { get; set; }
        public int CreatedBy { get; set; }
        public DateTime CreatedAt { get; set; }
        public int? UpdatedBy { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}
