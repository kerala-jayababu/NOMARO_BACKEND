using System.ComponentModel.DataAnnotations;

namespace Georgetown_Internationsl_Academy.API.DTO
{
    public class EmployeeLeaveConfigPostDto
    {
        [Key]
        public int IdEmployeeLeaveConfig { get; set; }   // 0 = insert

        public int IdEmployee { get; set; }
        public int IdLeaveTemplate { get; set; }

        public DateTime EffectiveFrom { get; set; }
        public DateTime? EffectiveTo { get; set; }
    }
}
