namespace Georgetown_Internationsl_Academy.API.DTO
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
}
