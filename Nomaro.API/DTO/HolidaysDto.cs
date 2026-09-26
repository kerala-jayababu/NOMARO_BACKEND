namespace Nomaro.API.DTO
{
    public class HolidaysDto
    {
        public int IdHoliday { get; set; }
        public DateTime HolidayDate { get; set; }
        public string? HolidayType { get; set; }
        public string? HolidayDescription { get; set; }
    }
}

