namespace Nomaro.API.DTO
{
    public class LeaveTemplateSaveResponseDto
    {
        public bool SuccessFlag { get; set; }
        public string Message { get; set; } = string.Empty;
        public int IdLeaveTemplate { get; set; }

        // optional full payload after save
        public LeaveTemplateWithDetailsDto? Template { get; set; }
    }
}

