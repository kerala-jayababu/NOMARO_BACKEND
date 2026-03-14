namespace Georgetown_Internationsl_Academy.API.DTO
{
    public class EmployeeChildrenDto
    {

        public int IdEmployeeChildren { get; set; }
        public int IdEmployee { get; set; }
        public string ChildName { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public string? Gender { get; set; }
        public string? CertificateNumber { get; set; }
        public string? DivisionNumber { get; set; }
    }
}
