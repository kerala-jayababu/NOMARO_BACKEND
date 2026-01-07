
using System.ComponentModel.DataAnnotations;
namespace Georgetown_Internationsl_Academy.API.DTO
{
    public class EmployeeLeaveConfigDetailsDto
    {
        [Key]
        public int IdEmployeeLeaveConfigDetails { get; set; }

        /// <summary>
        /// Employee-wise leave config header ID
        /// </summary>
        public int IdEmployeeLeaveConfig { get; set; }

        /// <summary>
        /// Leave template detail reference
        /// </summary>
        public int IdLeaveTemplateDetail { get; set; }

        /// <summary>
        /// Leave type reference
        /// </summary>
        public int IdLeaveType { get; set; }

        /// <summary>
        /// Allocated days for the year (from template)
        /// </summary>
        public int AllocatedDaysInYear { get; set; }

        /// <summary>
        /// Carry forward days from previous year
        /// </summary>
        public int CarryForwardDays { get; set; } = 0;

        /// <summary>
        /// Total allocated days (Allocated + Carry Forward)
        /// </summary>
        public int TotalAllocatedDays { get; set; }

        /// <summary>
        /// Used leave days
        /// </summary>
        public int UsedLeaveDays { get; set; } = 0;

        /// <summary>
        /// Balance available leave days
        /// </summary>
        public int BalanceLeaveDays { get; set; } = 0;
    }

    public class EmployeeLeaveConfigDetailsPostDto
    {
        [Key]
        public int IdEmployeeLeaveConfigDetails { get; set; }

        /// <summary>
        /// Employee-wise leave config header ID
        /// </summary>
        public int IdEmployeeLeaveConfig { get; set; }

        /// <summary>
        /// Leave template detail reference
        /// </summary>
        public int IdLeaveTemplateDetail { get; set; }

        /// <summary>
        /// Leave type reference
        /// </summary>
        public int IdLeaveType { get; set; }

        /// <summary>
        /// Allocated days for the year (from template)
        /// </summary>
        public int AllocatedDaysInYear { get; set; }

      }
}
