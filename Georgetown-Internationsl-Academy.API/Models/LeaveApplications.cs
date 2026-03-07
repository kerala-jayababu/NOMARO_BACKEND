
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace Georgetown_Internationsl_Academy.API.DTO
{
    public class LeaveApplications
    {
        [Key]       
        public int IdLeaveApplication { get; set; }
        public int IdEmployee { get; set; }
        public int? IdLeaveTemplateDetail { get; set; }
        public int IdLeaveType { get; set; }
        public string LeaveTypeName { get; set; }
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
        public string ApprovalStatus { get; set; }
        public int? ApprovedBy { get; set; }
        public bool? IsSalaryDeducted { get; set; }
        public int? IdEmployeeSalary { get; set; }
        public int? IdSalaryMonth { get; set; }
        public decimal? DeductedAmount { get; set; }
        public string? LeaveAttachmentFilePath {get;set;}
        public int? IdYear { get; set; }
        public string? LeaveApprovalDetails { get; set; }
        public int? BambooHRLeaveRequestID { get; set; }
    }
}
