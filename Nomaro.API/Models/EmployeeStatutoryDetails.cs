using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Nomaro.API.Models
{
    /// <summary>PF / ESI / PT / LWF details of an employee (one row per employee).</summary>
    public class EmployeeStatutoryDetails
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public int IdEmployee { get; set; }
        public string? PAN { get; set; }
        public string? UAN { get; set; }
        public string? PFNumber { get; set; }
        public bool IsPFApplicable { get; set; }
        public bool PFOnActualWage { get; set; }
        public bool IsEPSApplicable { get; set; }
        public decimal? VPFRate { get; set; }
        public string? ESINumber { get; set; }
        public bool IsESIApplicable { get; set; }
        public bool IsDisabled { get; set; }
        public int? IdPTState { get; set; }
        public bool IsPTApplicable { get; set; }
        public bool IsLWFApplicable { get; set; }
        public string? Gender { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public int? CreatedBy { get; set; }
        public DateTime? CreatedOn { get; set; }
        public int? ModifiedBy { get; set; }
        public DateTime? ModifiedOn { get; set; }
    }
}
