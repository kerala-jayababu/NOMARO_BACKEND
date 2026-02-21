namespace Georgetown_Internationsl_Academy.API.DTO.Time___Attendance.Shift
{
    public class ClockInOutDto
    {
        public int IdEmployee { get; set; }
        public int? IdClockDetails { get; set; }
        public string EmployeeCode { get; set; }
        public string EmailID { get; set; }
        public string EmployeeName { get; set; }
        public int IdDepartment { get; set; }
        public string DepartmentCode { get; set; }
        public string DepartmentName { get; set; }
        public int IdDesignation { get; set; }
        public string DesignationName { get; set; }
        public DateTime ClockDate { get; set; }
        public string InTime { get; set; }
        public string OUTTime { get; set; }
        public decimal? TotalINHours { get; set; }
        public string TotalHoursText { get; set; }
        public string ClockType { get; set; }
        public string StatusDetails { get; set; }
        public string Remarks { get; set; }

    }
}
