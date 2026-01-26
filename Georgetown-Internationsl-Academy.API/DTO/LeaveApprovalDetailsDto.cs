namespace Georgetown_Internationsl_Academy.API.DTO
{
    public class LeaveApprovalDetailsDto
    {
        public int Level { get; set; }
        public string Type { get; set; }   // REPOFFICER / ROLE
        public int Id { get; set; }
        public string Name { get; set; }

        public string Status { get; set; }        // PENDING / APPROVED / REJECTED
        public DateTime? StatusDate { get; set; } // null until action
    }
}
