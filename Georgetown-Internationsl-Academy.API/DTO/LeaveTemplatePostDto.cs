namespace Georgetown_Internationsl_Academy.API.DTO
{
    public class LeaveTemplatePostDto
    {
        // Header
        public int IdLeaveTemplate { get; set; }          // 0 = insert, >0 = update
        public string LeaveTemplateName { get; set; } = string.Empty;
        public string? LeaveTemplateDesc { get; set; }
        public int IdYear { get; set; }
        public int? IdAnnualLeaveTypeConfig { get; set; } // optional
        public bool IsActive { get; set; }

        // Detail Lines
        public List<LeaveTemplateDetailPostDto> Details { get; set; } = new();
    }

    public class LeaveTemplateDetailPostDto
    {
        public int IdLeaveTemplateDetails { get; set; }   // 0 = insert, >0 = update
        public int IdLeaveType { get; set; }
        public int? IdAnnualLeaveTypeConfig { get; set; } // optional
        public int NoOfDaysInYear { get; set; }
    }
}
