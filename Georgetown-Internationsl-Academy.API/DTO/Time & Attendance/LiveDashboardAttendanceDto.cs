namespace Georgetown_Internationsl_Academy.API.DTO.Time___Attendance
{
    public class LiveDashboardAttendanceDto
    {
        public int TotalEmployees { get; set; }
        public int ClockedInTotal { get; set; }
        public int OntimeClockIn { get; set; }
        public int LateClockIn { get; set; }
        public int PresentOnSite { get; set; }
        public int PresentOffSite { get; set; }
        public int AbsentTotal { get; set; }
        public int Authorized { get; set; }
        public int UnAuthorized { get; set; }
    }
}

