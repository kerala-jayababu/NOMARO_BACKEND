using System.ComponentModel.DataAnnotations;

namespace Georgetown_Internationsl_Academy.API.Models
{
    public class EmployeeChildren
    {
        [Key]
        public int IdEmployeeChildren { get; set; }

        public int IdEmployee { get; set; }

        [Required]
        [StringLength(80)]
        public string ChildName { get; set; }

        public DateTime? DateOfBirth { get; set; }

        [StringLength(20)]
        public string? Gender { get; set; }

        [StringLength(20)]
        public string? CertificateNumber { get; set; }

        [StringLength(20)]
        public string? DivisionNumber { get; set; }
    }
}
