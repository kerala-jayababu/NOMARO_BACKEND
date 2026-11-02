using Nomaro.API.DTO;

namespace Nomaro.API.Services.Interface
{
    public interface IEmployeeCalendarService
    {
        Task<CelebrationsDto> GetCelebrations(int upcomingDays);
        Task<DateTime> SendCongratulationsEmail(SendCongratulationsDto dto, int idLoggedInEmployee);
        Task<IEnumerable<HolidayCalendarDto>> GetHolidayCalendar(DateTime fromDate, DateTime toDate);
    }
}
