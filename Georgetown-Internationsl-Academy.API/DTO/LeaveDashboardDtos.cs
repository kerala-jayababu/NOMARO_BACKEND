namespace Georgetown_Internationsl_Academy.API.DTO
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

}
