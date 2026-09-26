using System;
using System.Collections.Generic;

namespace Nomaro.API.DTO
{
    public class LeaveApplicationDetailsDto
    {
        public int IdLeaveApplication { get; set; }
        public int IdEmployee { get; set; }
        public int IdLeaveType { get; set; }

        public string? LeaveTypeName { get; set; }

        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }

        public bool IsHalfDay { get; set; }
        public char? HalfDayType { get; set; }

        public decimal TotalLeaveDays { get; set; }
        public string? Reason { get; set; }

        public DateTime AppliedOn { get; set; }
        public string? ApplicationStatus { get; set; }

        public DateTime? CancelledDate { get; set; }
        public string? ReasonForCancellation { get; set; }

        public string? ApprovalStatus { get; set; }
        public int? ApprovedBy { get; set; }

        public bool? IsSalaryDeducted { get; set; }
        public int? IdEmployeeSalary { get; set; }
        public int? IdSalaryMonth { get; set; }
        public decimal? DeductedAmount { get; set; }
        public string? LeaveApprovalDetails { get; set; }

        public List<LeaveApplicationDocumentDetailsDto> Documents { get; set; } = new();
    }

    public class LeaveApplicationDocumentDetailsDto
    {
        public int IdLeaveApplicationDocument { get; set; }
        public int IdLeaveApplication { get; set; }

        public string? FileType { get; set; }

        public string? FileName { get; set; }          // ✅ original name
        public string? StoredFilePath { get; set; }    // ✅ actual path in db
        public DateTime UploadedAt { get; set; }

        public byte[]? FileBinary { get; set; }        // ✅ binary
    }
}

