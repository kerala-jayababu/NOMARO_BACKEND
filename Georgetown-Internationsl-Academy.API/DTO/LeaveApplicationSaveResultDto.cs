namespace Georgetown_Internationsl_Academy.API.DTO
{
    public class LeaveApplicationSaveResultDto
    {
        public bool SuccessFlag { get; set; }
        public string Message { get; set; } = string.Empty;

        public int IdLeaveApplication { get; set; }

        public decimal TotalLeaveDays { get; set; }
        public string ApprovalStatus { get; set; } = "Pending";
        public string ApplicationStatus { get; set; } = "Submitted";
        public DateTime AppliedOn { get; set; }
    }
}
