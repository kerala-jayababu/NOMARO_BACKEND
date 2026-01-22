namespace Georgetown_Internationsl_Academy.API.DTO
{
    public class LeaveDashboardDto
    {
        public int IdEmployee { get; set; }
        public int IdLeaveType { get; set; }
        public string LeaveTypeCode { get; set; }
        public string LeaveTypeName{ get; set; }
        public decimal TotalAllocated { get; set; }
        public decimal TotalTaken { get; set; }
        public decimal TotalApproved { get; set; }
        public decimal TotalRejected { get; set; }
        public decimal TotalBalance { get; set; }

    }
}
