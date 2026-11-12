namespace Nomaro.API.DTO
{
    public class EmployeeFamilyMemberDto
    {
        public int IdEmployeeFamilyMember { get; set; }
        public int IdEmployee { get; set; }
        public string Relationship { get; set; }
        public string? RelationshipLabel { get; set; }
        public string MemberName { get; set; }
        public string? Gender { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public string? ContactNumber { get; set; }
        public string? Occupation { get; set; }
        public bool IsDependent { get; set; }
        public bool IsNominee { get; set; }
        public decimal? NomineePercentage { get; set; }
        public DateTime? MarriageDate { get; set; }
        public string? CertificateNumber { get; set; }
        public string? DependentEmiratesId { get; set; }
        public string? DivisionNumber { get; set; }
        public string? Remarks { get; set; }
    }
}
