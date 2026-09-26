using Nomaro.API.DTO;

namespace Nomaro.API.Services.Interface
{
    public interface IHolidayServices
    {
        Task<IEnumerable<HolidaysDto>> GetHolidaysInAnYear(int Year);
        Task<bool> AddOrUpdateHoliday(HolidaysDto holidayDetail);
        Task<bool> DeleteHoliday(int IdHoliday);


    }
}

