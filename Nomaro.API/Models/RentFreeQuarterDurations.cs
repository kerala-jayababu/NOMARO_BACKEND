using System.ComponentModel.DataAnnotations;

namespace Nomaro.API.Models
{
    public class RentFreeQuarterDurations
    {
        [Key]
        public int IdRentFreeQuarterDuration { get; set; }
        public string RentFreeQuarterDurationName { get; set; }
        public int MonthCount { get; set; }
    }
}

