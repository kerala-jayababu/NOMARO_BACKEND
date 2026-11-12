using System.ComponentModel.DataAnnotations;

namespace Nomaro.API.Models
{
    // Spouse, children, parents and other family members of an employee (replaces EmployeeChildren).
    public class EmployeeFamilyMembers
    {
        [Key]
        public int IdEmployeeFamilyMember { get; set; }
        public int IdEmployee { get; set; }
        public string Relationship { get; set; }          // SPOUSE, SON, DAUGHTER, FATHER, MOTHER, FATHERINLAW, MOTHERINLAW, BROTHER, SISTER, OTHER
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
        public int? SourceIdEmployeeChildren { get; set; }  // set for rows copied from EmployeeChildren
        public int? CreatedBy { get; set; }
        public DateTime CreatedDateTime { get; set; }
        public int? ModifiedBy { get; set; }
        public DateTime? ModifiedDateTime { get; set; }
    }
}
