namespace Nomaro.API.DTO
{
    public class EmployeeLeaveSetupDto
    {
        public int IdEmployeeLeaveConfig { get; set; }
        public int IdEmployee { get; set; }
        public string EmployeeName { get; set; }
        public string DepartmentName { get; set; }
        public string DesignationName { get; set; }
        public DateTime JoingDate { get; set; }
        public int IdLeaveTemplate { get; set; }
        public string LeaveTemplateName { get; set; }
        public int IdYear { get; set; }
        public DateTime EffectiveFrom { get; set; }
        public DateTime? EffectiveTo { get; set; }

        public int CreatedBy { get; set; }
        public DateTime CreatedAt { get; set; }
        public int? UpdatedBy { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public string? ApprovalStatus { get; set; }
        public int? IdApprovedBy { get; set; }
        // Details
        public List<EmployeeLeaveSetupDetailDto> Details { get; set; } = new();
    }

    public class EmployeeLeaveSetupDetailDto
    {
        public int IdEmployeeLeaveConfigDetail { get; set; }
        public int IdEmployeeLeaveConfig { get; set; }
        public int IdLeaveTemplateDetail { get; set; }
        public int IdLeaveType { get; set; }
        public string? LeaveTypeName { get; set; }
        public string? LeaveCode { get; set; }
        public decimal AllocatedDaysInYear { get; set; }
        public decimal CarryForwardDays { get; set; } = 0;
        public decimal TotalAllocatedDays { get; set; }
        public decimal UsedLeaveDays { get; set; } = 0;
        public decimal BalanceLeaveDays { get; set; } = 0;

        //Properties from Leave Template Details
        public string ApplicableGender { get; set; } = "BOTH";
        public bool IsPaid { get; set; } = true;
        public decimal? SalaryDeductionPercent { get; set; } = 0;
        public int? SalaryDeductAfterDays { get; set; }

        public bool AllowHalfDay { get; set; } = true;
        public bool RequiresApproval { get; set; } = false;
        public int? RequiredApprovalLevel { get; set; } = 0;
        public bool RequiresDocument { get; set; } = false;
        public int? DocumentRequiredAfterDays { get; set; } = 0;
        public bool IsCarryForwardAllowed { get; set; } = false;
        public int? MaxCarryForwardDays { get; set; } = 0;
        public bool IncludeHolidaysBetween { get; set; } = true;
        public int MaxLeavesPerYear { get; set; }
        public int? MaxLeavesPerMonth { get; set; }
        public bool AllowBackdatedLeave { get; set; } = true;
        public int? BackdateLimitDays { get; set; }
    }
}

