namespace Nomaro.API.DTO
{
    public class MissingEntryForApprovalDto
    {
        public int IdClockInDetail { get; set; }
        public int Idemployee { get; set; }
        public string EmployeeName { get; set; }
        public string DesignationName { get; set; }
        public string DepartmentName { get; set; }
        public DateTime MissingEntryDate { get; set; }
        public DateTime? MissingManualEntryTime{ get; set; }
        public string? Reason { get; set; }
        public string ManualEntryType { get; set; }
        public string StatusDetails { get; set; }
        public string ApprovalStatus { get; set; }
    }

    public class ForgotCardEntryForApprovalDto
    {
        public int IdClockInDetail { get; set; }
        public int Idemployee { get; set; }
        public string EmployeeName { get; set; }
        public string DesignationName { get; set; }
        public string DepartmentName { get; set; }
        public DateTime ForgotCardEntryDate { get; set; }
        public DateTime? EntryTime { get; set; }
        public DateTime? ExitTime { get; set; }
        public string? Reason { get; set; }
        public string StatusDetails { get; set; }
        public string ApprovalStatus { get; set; }
    }
}

