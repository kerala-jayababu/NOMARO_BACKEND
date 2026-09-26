namespace Nomaro.API.DTO
{
    public class TodayAtAGlanceDto
    {
        public DateTime Date { get; set; }
        public int OnLeaveToday { get; set; }
        public int UnauthorizedAbsentToday { get; set; }
        public int WorkFromHomeToday { get; set; }
    }

    public class TodayEmployeeDto
    {
        public int IdEmployee { get; set; }
        public string EmployeeCode { get; set; }
        public string EmployeeName { get; set; }
        public string Department { get; set; }
        public string Designation { get; set; }
        public string PhoneNumber1 { get; set; }
        public string PhoneNumber2 { get; set; }
        public string WhatsAppNumber { get; set; }
    }

    public class LeaveRequestRowDto
    {
        public int IdLeaveApplication { get; set; }
        public int IdEmployee { get; set; }
        public string EmployeeCode { get; set; }
        public string EmployeeName { get; set; }
        public string Department { get; set; }
        public string Designation { get; set; }
        public string LeaveTypeName { get; set; }
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public decimal TotalLeaveDays { get; set; }
        public string ApprovalStatus { get; set; }
        public string ApplicationStatus { get; set; }
        public string PhoneNumber1 { get; set; }
        public string PhoneNumber2 { get; set; }
    }

    // ── KPI Summary ───────────────────────────────────────
    public class LeaveKpiSummaryDto
    {
        public int TotalEmployees { get; set; }
        public decimal TotalLeaveDays { get; set; }
        public decimal AvgLeavePerEmployee { get; set; }
        public string HighestLeaveType { get; set; }
        public decimal HighestLeaveTypeDays { get; set; }
    }

    // ── Leave by Type (Pie Chart) ─────────────────────────
    public class LeaveByTypeDto
    {
        public string LeaveTypeName { get; set; }
        public string LeaveCode { get; set; }
        public decimal TotalDays { get; set; }
    }

    // ── Monthly Trend (Line Chart) ────────────────────────
    public class MonthlyLeaveTrendDto
    {
        public int Month { get; set; }   // 1–12
        public string MonthName { get; set; }   // "Jan", "Feb" …
        public decimal TotalLeaveDays { get; set; }
    }

    // ── Department Summary (Bar Chart + Table) ────────────
    public class DepartmentLeaveDto
    {
        public string DepartmentName { get; set; }
        public decimal TotalLeaveDays { get; set; }
        public IEnumerable<LeaveTypeBreakdownDto> LeaveBreakdown { get; set; }
    }

    // ── Designation Summary (Bar Chart + Table) ───────────
    public class DesignationLeaveDto
    {
        public string DesignationName { get; set; }
        public decimal TotalLeaveDays { get; set; }
        public IEnumerable<LeaveTypeBreakdownDto> LeaveBreakdown { get; set; }
    }

    // ── Shared: Leave Type Breakdown (used in Dept/Desig) ─
    public class LeaveTypeBreakdownDto
    {
        public string LeaveTypeName { get; set; }
        public string LeaveCode { get; set; }
        public decimal TotalDays { get; set; }
    }

    // ── Employee Leave Details (Employee Tab Table) ────────
    public class EmployeeLeaveDetailDto
    {
        public int IdEmployee { get; set; }
        public string EmployeeName { get; set; }
        public string DepartmentName { get; set; }
        public string DesignationName { get; set; }
        public decimal TotalLeaveDays { get; set; }
        public IEnumerable<LeaveTypeBreakdownDto> LeaveBreakdown { get; set; }
    }

}

