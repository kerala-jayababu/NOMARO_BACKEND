namespace Georgetown_Internationsl_Academy.API.DTO
{
    public class LeaveTemplateDto
    {
        public int IdLeaveTemplate { get; set; }
        public string LeaveTemplateName { get; set; } = string.Empty;
        public int? IdAnnualLeaveTypeConfig { get; set; }
        public bool IsActive { get; set; }
        public int CreatedBy { get; set; }
        public DateTime CreatedAt { get; set; }
        public int? UpdatedBy { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public AnnualLeaveTypeConfigDto? AnnualLeaveTypeConfig { get; set; }
    }
}
