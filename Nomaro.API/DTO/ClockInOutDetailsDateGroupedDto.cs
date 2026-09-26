using Nomaro.API.Models.Time___Attendance.Shift;
using Org.BouncyCastle.Bcpg.OpenPgp;

namespace Nomaro.API.DTO
{
    public class ClockInOutDetailsDateGroupedDto
    {
     
        public int OrderNumber { get; set; }
        public DateTime ClockDate { get; set; }
        public List<EmployeeClockDetails>? ClockDetails { get; set; }
    }

    public class EmployeeClockDetails   
    {
        public int IdClockDetails { get; set; }
        public DateTime? INTime { get; set; }
        public DateTime? OUTTime { get; set; }
        public decimal? TotalINHours { get; set; }
        public int? TotalInminutes { get; set; }
        public string? TotalInHoursText { get; set; }
        public string? StatusDetails { get; set; }
        public string? Remarks { get; set; }

    }
}

