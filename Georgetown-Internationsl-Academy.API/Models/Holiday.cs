using System.ComponentModel.DataAnnotations;

namespace YourNamespace.Models
{
    public class Holiday
    {

        [Key]
        public int IdHoliday { get; set; } 
        public DateTime HolidayDate { get; set; } 
        public string HolidayType { get; set; } = string.Empty;
        public string? HolidayDescription { get; set; }
    }
}
