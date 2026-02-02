namespace Georgetown_Internationsl_Academy.API.DTO
{
    public class LeaveApprovalDetailsDto
    {
        public int level { get; set; }
        public string type { get; set; }   // REPOFFICER / ROLE
        public int id { get; set; }
        public string name { get; set; }

        public string status { get; set; }        // PENDING / APPROVED / REJECTED
        public DateTime? statusDate { get; set; } // null until action
    }
}
