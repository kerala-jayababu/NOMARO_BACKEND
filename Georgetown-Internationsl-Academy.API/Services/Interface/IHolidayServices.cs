using Georgetown_Internationsl_Academy.API.DTO;

namespace Georgetown_Internationsl_Academy.API.Services.Interface
{
    public interface IHolidayServices
    {
        Task<IEnumerable<HolidaysDto>> GetHolidaysInAnYear(int Year);
        Task<bool> AddOrUpdateHoliday(HolidaysDto holidayDetail);


    }
}
