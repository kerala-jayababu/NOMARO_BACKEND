using System.ComponentModel.DataAnnotations;

namespace Nomaro.API.Models
{
    public class HolidayTypeEntity
    {
        [Key]
        public string HolidayType { get; set; }
        public string HolidayTypeName { get; set; } = string.Empty;
    }
}

