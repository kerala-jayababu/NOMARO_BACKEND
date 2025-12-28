namespace Georgetown_Internationsl_Academy.API.DTO
{
    public class EmployeeLeaveSetupDto
    {
        public int IdEmployeeLeaveConfig { get; set; }
        public int IdEmployee { get; set; }
        public int IdLeaveTemplate { get; set; }
        public DateTime EffectiveFrom { get; set; }
        public DateTime? EffectiveTo { get; set; }

        public int CreatedBy { get; set; }
        public DateTime CreatedAt { get; set; }
        public int? UpdatedBy { get; set; }
        public DateTime? UpdatedAt { get; set; }

        // Details
        public List<EmployeeLeaveSetupDetailDto> Details { get; set; } = new();
    }

    public class EmployeeLeaveSetupDetailDto
    {
        public int IdEmployeeLeaveConfigDetail { get; set; }
        public int IdEmployeeLeaveConfig { get; set; }
        public int IdLeaveType { get; set; }

        public int AllocatedDays { get; set; }
        public int UsedLeaveDays { get; set; }
        public int BalanceLeaveDays { get; set; }

        // ✅ Optional: If you want leave type name also
        public string? LeaveTypeName { get; set; }
        public string? LeaveCode { get; set; }
    }
}
