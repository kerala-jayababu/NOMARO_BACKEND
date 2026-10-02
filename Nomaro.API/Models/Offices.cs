using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Nomaro.API.Models
{
    public class Offices
    {
        [Key]
        public int IdOffice { get; set; }
        public string OfficeCode { get; set; }
        public string OfficeName { get; set; }
        public int IdOfficeType { get; set; }
        public int? IdParentOffice { get; set; }
        public string? AddressLine1 { get; set; }
        public string? AddressLine2 { get; set; }
        public string? City { get; set; }
        public string? District { get; set; }
        public string? State { get; set; }
        public string? Country { get; set; }
        public string? PinCode { get; set; }
        public string? PhoneNumber { get; set; }
        public string? EmailId { get; set; }
        public string? GSTIN { get; set; }
        public int? IdOfficeHead { get; set; }
        public int? IdShiftSchedule { get; set; }
        public int? ShiftManager { get; set; }
        public DateTime? OpenedDate { get; set; }
        public DateTime? ClosedDate { get; set; }
        public bool IsActive { get; set; }
        public int? IdCreatedBy { get; set; }
        public DateTime CreatedDateTime { get; set; }
        public int? IdModifiedBy { get; set; }
        public DateTime? ModifiedDateTime { get; set; }

        // Navigation Property
        [ForeignKey("IdOfficeType")]
        public virtual OfficeTypes OfficeType { get; set; }
    }
}
