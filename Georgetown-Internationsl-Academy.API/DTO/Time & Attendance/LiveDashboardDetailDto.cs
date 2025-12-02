using System;

namespace Georgetown_Internationsl_Academy.API.DTO.Time___Attendance
{
    public class LiveDashboardDetailDto
    {
        public int IdEmployee { get; set; }
        public string? EmployeeCode { get; set; }
        public string? EmployeeName { get; set; }
        public string? DepartmentName { get; set; }
        public string? DesignationName { get; set; }
        public string? EmailID { get; set; }
        public string? PhoneNumber1 { get; set; }
        public string? EmpStatus { get; set; }
        public DateTime? FirstCheckInTime { get; set; }
        public DateTime? LastCheckOutTime { get; set; }
    }
}

