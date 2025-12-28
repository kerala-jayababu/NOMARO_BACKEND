using System;

namespace Georgetown_Internationsl_Academy.API.DTO
{
    public class CancelLeaveApplicationResultDto
    {
        public bool SuccessFlag { get; set; }
        public string Message { get; set; } = string.Empty;

        public int IdLeaveApplication { get; set; }
        public string ApplicationStatus { get; set; } = string.Empty;

        public DateTime? CancelledDate { get; set; }
        public string? ReasonForCancellation { get; set; }
    }
}
