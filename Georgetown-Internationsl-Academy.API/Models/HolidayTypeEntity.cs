using System.ComponentModel.DataAnnotations;

namespace Georgetown_Internationsl_Academy.API.Models
{
    public class HolidayTypeEntity
    {
        [Key]
        public string HolidayType { get; set; }
        public string HolidayTypeName { get; set; } = string.Empty;
    }
}
