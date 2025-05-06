using AutoMapper;
using Georgetown_International_Academy.API.Database;
using Georgetown_Internationsl_Academy.API.DTO;
using Georgetown_Internationsl_Academy.API.Models;
using Georgetown_Internationsl_Academy.API.Services.Interface;
using Microsoft.EntityFrameworkCore;

namespace Georgetown_Internationsl_Academy.API.Services.Implimentation
{
    public class HolidayServices: IHolidayServices
    {
        private readonly ApplicationDBContext _dbContext;
        private readonly IMapper _mapper;
        private readonly ILogger<BankServices> _logger;

        public HolidayServices(ApplicationDBContext dbContext, IMapper mapper, ILogger<BankServices> logger)
        {
            _dbContext = dbContext;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<IEnumerable<HolidaysDto>> GetHolidaysInAnYear(int Year)
        {
            try
            {
                var holidayList = await _dbContext.Holidays.OrderBy(b => b.HolidayDate.Year == Year).ToListAsync();
                var HolidaysDtos = _mapper.Map<IEnumerable<HolidaysDto>>(holidayList);

                return HolidaysDtos;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching the list of Banks.");
                throw;
            }
        }

        public async Task<bool> AddOrUpdateHoliday(HolidaysDto holidayDetail)
        {
            using var transaction = await _dbContext.Database.BeginTransactionAsync();

            try
            {
                if (holidayDetail.IdHoliday > 0)
                {
                    var holidayDet = await _dbContext.Holidays.FirstOrDefaultAsync(h => h.IdHoliday == holidayDetail.IdHoliday);
                    if (holidayDet != null)
                    {
                        holidayDet.HolidayDate = holidayDetail.HolidayDate;
                        holidayDet.HolidayDescription = holidayDetail.HolidayDescription;
                        holidayDet.HolidayType = holidayDetail.HolidayType;

                        _dbContext.Holidays.Update(holidayDet);
                        await _dbContext.SaveChangesAsync();
                    }
                }
                else
                {
                    var holidayDet = new Models.Holiday
                    {
                        HolidayDate = holidayDetail.HolidayDate,
                        HolidayDescription = holidayDetail.HolidayDescription,
                        HolidayType = holidayDetail.HolidayType
                    };
                    _dbContext.Holidays.Add(holidayDet);
                    await _dbContext.SaveChangesAsync();
                }

                await transaction.CommitAsync();

                return true; 
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error adding, updating, or removing Holiday.");
                throw new Exception("An error occurred while processing Holiday. Please try again.");
            }
        }


    }
}
