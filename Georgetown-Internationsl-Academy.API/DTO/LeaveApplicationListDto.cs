namespace Georgetown_Internationsl_Academy.API.DTO
{
    public class LeaveApplicationListDto
    {
        public int IdLeaveApplication { get; set; }
        public int IdEmployee { get; set; }

        public string? EmployeeName { get; set; } // optional
        public string? DepartmentName { get; set; } // optional
        public string? DesignationName { get; set; } // optional
        public int? IdLeaveTemplateDetail { get; set; }
        public int IdLeaveType { get; set; }
        public string? LeaveTypeName { get; set; }
        public string? Reason { get; set; }
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }

        public decimal TotalLeaveDays { get; set; }
        public string? ApprovalStatus { get; set; }
        public string? ApplicationStatus { get; set; }
        public DateTime AppliedOn { get; set; }
        public string? LeaveApprovalDetails { get; set; }
        public string? ActionStatusByUser { get; set; }
        public DateTime? ActionStatusDateByUser { get; set; }


    }
}
