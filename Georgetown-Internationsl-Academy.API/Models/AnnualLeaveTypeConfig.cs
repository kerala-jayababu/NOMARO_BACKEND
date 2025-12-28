
using System;
using System.ComponentModel.DataAnnotations;
namespace Georgetown_Internationsl_Academy.API.DTO
{
    public class AnnualLeaveTypeConfig
    {
        [Key]
        public int IdAnnualLeaveTypeConfig { get; set; }
        public int IdLeaveType { get; set; }
        public string LeaveTypeName { get; set; }
        public string LeaveCode { get; set; }
        public int IdYear { get; set; }
        public DateTime EffectiveFrom { get; set; }
        public DateTime EffectiveTo { get; set; }
        public bool IsPaid { get; set; }
        public decimal SalaryDeductionPercent { get; set; }
        public bool AllowHalfDay { get; set; }
        public bool RequiresApproval { get; set; }
        public int RequiredApprovalLevel { get; set; }
        public bool RequiresDocument { get; set; }
        public int? DocumentRequiredAfterDays { get; set; }
        public bool IsCarryForwardAllowed { get; set; }
        public int? MaxCarryForwardDays { get; set; }
        public string ApplicableGender { get; set; }
        public bool IsActive { get; set; }
        public bool IncludeHolidaysBetween { get; set; }
        public int MaxLeavesPerYear { get; set; }
        public int MaxLeavesPerMonth { get; set; }
        public bool AllowBackdatedLeave { get; set; }
        public int? BackdateLimitDays { get; set; }
        public int CreatedBy { get; set; }
        public DateTime CreatedAt { get; set; }
        public int? UpdatedBy { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}
