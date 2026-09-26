namespace Nomaro.API.DTO
{
    public class LeaveTemplateWithDetailsDto
    {
        // ✅ Header
        public int IdLeaveTemplate { get; set; }
        public string LeaveTemplateName { get; set; } = string.Empty;
        public string? LeaveTemplateDesc { get; set; }
        public bool IsActive { get; set; }
        public int IdYear { get; set; }
        public int CreatedBy { get; set; }
        public DateTime CreatedAt { get; set; }
        public int? UpdatedBy { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public List<LeaveTemplateDetailsDto> Details { get; set; } = new();
    }
   
}

