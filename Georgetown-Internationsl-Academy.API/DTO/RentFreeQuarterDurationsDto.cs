using System.ComponentModel.DataAnnotations;

namespace Georgetown_Internationsl_Academy.API.DTO
{
    public class RentFreeQuarterDurationsDto
    {
        
        public int IdRentFreeQuarterDuration { get; set; }
        public string RentFreeQuarterDurationName { get; set; }
        public int MonthCount { get; set; }
    }
}
