using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Nomaro.API.DTO
{
    public class OfficeDto
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
        public DateTime? OpenedDate { get; set; }
        public DateTime? ClosedDate { get; set; }
        public bool IsActive { get; set; }

        [NotMapped]
        public OfficeShiftScheduleDto? ShiftSchedule { get; set; }

        [NotMapped]
        public string? OfficeTypeCode { get; set; }
        [NotMapped]
        public string? OfficeTypeName { get; set; }
        [NotMapped]
        public int? HierarchyLevel { get; set; }
        [NotMapped]
        public string? ParentOfficeCode { get; set; }
        [NotMapped]
        public string? ParentOfficeName { get; set; }
        [NotMapped]
        public string? OfficeHeadName { get; set; }
    }
}
