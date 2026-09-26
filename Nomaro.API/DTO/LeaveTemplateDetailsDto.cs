
using System.ComponentModel.DataAnnotations;
namespace Nomaro.API.DTO
{
    public class LeaveTemplateDetailsDto
    {
        [Key]
     
            public int IdLeaveTemplateDetails { get; set; }
            public int IdLeaveTemplate { get; set; }
            public int IdLeaveType { get; set; }
            public string LeaveTypeName { get; set; }
            public string LeaveCode { get; set; }
            public int IdYear { get; set; }
            public DateTime EffectiveFrom { get; set; }
            public DateTime EffectiveTo { get; set; }

            /// <summary>
            /// MALE, FEMALE, BOTH
            /// </summary>
            public string ApplicableGender { get; set; } = "BOTH";

            /// <summary>
            /// 1 = Paid, 0 = Unpaid
            /// </summary>
            public bool IsPaid { get; set; } = true;

            /// <summary>
            /// Percentage (0–100). Nullable as per DB
            /// </summary>
            public decimal? SalaryDeductionPercent { get; set; } = 0;
        public int? SalaryDeductAfterDays { get; set; }

        public bool AllowHalfDay { get; set; } = true;

            public bool RequiresApproval { get; set; } = false;

            /// <summary>
            /// Approval level (1, 2, etc.) – Nullable
            /// </summary>
            public int? RequiredApprovalLevel { get; set; } = 0;

            public bool RequiresDocument { get; set; } = false;

            /// <summary>
            /// Document required after N days – Nullable
            /// </summary>
            public int? DocumentRequiredAfterDays { get; set; } = 0;

            public bool IsCarryForwardAllowed { get; set; } = false;

            /// <summary>
            /// Carry forward cap – Nullable
            /// </summary>
            public int? MaxCarryForwardDays { get; set; } = 0;

            public bool IncludeHolidaysBetween { get; set; } = true;

            public int MaxLeavesPerYear { get; set; }

            /// <summary>
            /// Nullable as per DB
            /// </summary>
            public int? MaxLeavesPerMonth { get; set; }

            public bool AllowBackdatedLeave { get; set; } = true;

            /// <summary>
            /// Max allowed backdate days – Nullable
            /// </summary>
            public int? BackdateLimitDays { get; set; }

            public List<LeaveWorkFlowDetailDto> leaveWorkFlowDetails { get; set; }
        }

   
}

