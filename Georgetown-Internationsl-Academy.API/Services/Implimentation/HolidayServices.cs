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
        private readonly IAuditService _auditService;

        public HolidayServices(ApplicationDBContext dbContext, IMapper mapper, ILogger<BankServices> logger, IAuditService auditService)
        {
            _dbContext = dbContext;
            _mapper = mapper;
            _logger = logger;
            _auditService = auditService;
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
                string holidType = new string(holidayDetail.HolidayType.Where(c => !char.IsWhiteSpace(c)).ToArray()).ToUpper();

                if (holidayDetail.IdHoliday > 0)
                {
                    var holidayDet = await _dbContext.Holidays.FirstOrDefaultAsync(h => h.IdHoliday == holidayDetail.IdHoliday);
                    if (holidayDet != null)
                    {
                        holidayDet.HolidayDate = holidayDetail.HolidayDate;
                        holidayDet.HolidayDescription = holidayDetail.HolidayDescription;
                        holidayDet.HolidayType = holidType;

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
                        HolidayType = holidType
                    };
                    _dbContext.Holidays.Add(holidayDet);
                    await _dbContext.SaveChangesAsync();
                }

                await transaction.CommitAsync();

                await _auditService.LogAuditAsync(
                    actionType: holidayDetail.IdHoliday > 0 ? "Update" : "Create",
                    entityName: "Holiday",
                    entityId: holidayDetail.IdHoliday,
                    actionDetails: new { after = holidayDetail });

                return true; 
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error adding, updating, or removing Holiday.");
                throw new Exception("An error occurred while processing Holiday. Please try again.");
            }
        }


        public async Task<bool> DeleteHoliday(int IdHoliday)
        {
            using var transaction = await _dbContext.Database.BeginTransactionAsync();

            try
            {
                if (IdHoliday > 0)
                {
                    var holidayDet = await _dbContext.Holidays.FirstOrDefaultAsync(h => h.IdHoliday == IdHoliday);
                    if (holidayDet != null)
                    {
                        _dbContext.Holidays.Remove(holidayDet);
                        await _dbContext.SaveChangesAsync();
                    }
                }
          
                await transaction.CommitAsync();

                return true;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error adding, Deleteting the record");
                throw new Exception("An error occurred while Deleteting Holiday. Please try again.");
            }
        }

    }
}
