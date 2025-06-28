using System.ComponentModel.DataAnnotations;

namespace Georgetown_Internationsl_Academy.API.Models.Time___Attendance.Bamboo_HR
{
    public class EmployeeLeave
    {
        [Key]
        public int IdEmployeeLeave { get; set; }
        public int IdEmployee { get; set; }
        public DateTime LeaveFromDate { get; set; }
        public DateTime LeaveToDate { get; set; }
        public decimal NoDays { get; set; }
        public DateTime AppliedDate { get; set; }
        public DateTime? ApprovedDate { get; set; }
        public string ApprovalStatus { get; set; }
        public int? ApprovedBy { get; set; }
        public int IdLeaveType { get; set; }
        public string LeaveTypeName { get; set; }
        public string? PayableStatus { get; set; }
        public int? IdSalaryMonthAdjusted { get; set; }
        public string? SalaryTransactionType { get; set; }
        public int? SalaryTransactionID { get; set; }
        public decimal? SalaryAmountAdjusted { get; set; }
        public int? BambooHRRequestId { get; set; }
    }
}
