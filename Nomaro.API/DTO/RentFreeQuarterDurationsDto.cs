using System.ComponentModel.DataAnnotations;

namespace Nomaro.API.DTO
{
    public class RentFreeQuarterDurationsDto
    {
        
        public int IdRentFreeQuarterDuration { get; set; }
        public string RentFreeQuarterDurationName { get; set; }
        public int MonthCount { get; set; }
    }
}

