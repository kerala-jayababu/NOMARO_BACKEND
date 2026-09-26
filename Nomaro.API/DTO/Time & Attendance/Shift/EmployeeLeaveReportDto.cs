namespace Nomaro.API.DTO.Time___Attendance.Shift
{
    public class EmployeeLeaveReportDto
    {
        public int IdEmployeeLeave { get; set; }
        public int IdEmployee { get; set; }
        public string EmployeeName { get; set; }
        public int IdDesignation { get; set; }
        public string DesignationName { get; set; }
        public int IdDepartment { get; set; }
        public string DepartmentName { get; set; }  
        public DateTime LeaveFromDate { get; set; }
        public DateTime LeaveToDate { get; set; }
        public decimal NoDays { get; set; }
        public DateTime AppliedDate { get; set; }
        public string ApprovalStatus { get; set; }
        public string LeaveTypeName { get; set; }
        public string PayableStatus { get; set; }
        public string SalaryTransactionType { get; set; }
        public string SalaryTransactionID { get; set; }
        public decimal? SalaryAmountAdjusted { get; set; }
    }

}

