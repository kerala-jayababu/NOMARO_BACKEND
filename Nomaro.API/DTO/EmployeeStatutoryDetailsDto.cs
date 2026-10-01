using System.ComponentModel.DataAnnotations.Schema;

namespace Nomaro.API.DTO
{
    public class EmployeeStatutoryDetailsDto
    {
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
        // Filled from Employees when not sent
        public string? Gender { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public int? CreatedBy { get; set; }
        public DateTime? CreatedOn { get; set; }
        public int? ModifiedBy { get; set; }
        public DateTime? ModifiedOn { get; set; }

        [NotMapped]
        public string? EmployeeCode { get; set; }
        [NotMapped]
        public string? EmployeeName { get; set; }
    }
}
