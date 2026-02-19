using System.ComponentModel.DataAnnotations;

namespace Georgetown_Internationsl_Academy.API.Models.Time___Attendance.Shift
{
    public class ClockInOutDetails
    {
        [Key]
        public int IdClockDetails { get; set; }
        public string DeviceUser { get; set; }
        public int IdEmployee { get; set; }
        public DateTime ClockDate { get; set; }
        public DateTime? INTime { get; set; }
        public DateTime? OUTTime { get; set; }
        public decimal? TotalINHours { get; set; }
        public int? TotalInMinutes { get; set; }
        public string? TotalInHoursText { get; set; }
        public string? StatusDetails { get; set; }
        public string? Remarks { get; set; }
        public string? MissingEntryApproveStatus { get; set; }
        public DateTime? MissingEntryApprovedDate { get; set; }
        public int? IdMissingEntryApprovedBy { get; set; }

    }
}
