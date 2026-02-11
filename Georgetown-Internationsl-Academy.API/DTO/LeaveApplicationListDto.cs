namespace Georgetown_Internationsl_Academy.API.DTO
{
    public class LeaveApplicationListDto
    {
        public int IdLeaveApplication { get; set; }
        public int IdEmployee { get; set; }

        public string? EmployeeCode { get; set; }
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
        public string LeaveApprovalHistories { get; set; }
        public bool HasDocuments { get; set; }

    }

    public class LeaveApprovalHistory
    {
        public int IdHistory { get; set; }
        public string? ActionBy { get; set; }
        public string? Status { get; set; }
        public DateTime? ActionDate { get; set; }
        public string? Remarks { get; set; }

    }

    public class LeaveApplicationDocumentDto
    {
        public int IdLeaveApplicationDocument { get; set; }
        public string FileType { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public string FileUrl { get; set; } = string.Empty;
        public DateTime UploadedAt { get; set; }
        public byte[]? FileBinary { get; set; }
    }
}
