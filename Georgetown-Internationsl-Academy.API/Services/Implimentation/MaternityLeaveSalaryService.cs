using AutoMapper;
using Georgetown_International_Academy.API.Database;
using Georgetown_Internationsl_Academy.API.DTO;
using Georgetown_Internationsl_Academy.API.Models;
using Georgetown_Internationsl_Academy.API.Services.Interface;
using Microsoft.EntityFrameworkCore;

public class MaternityLeaveSalaryService : IMaternityLeaveSalaryService
{
    private readonly ApplicationDBContext _dbContext;
    private readonly IMapper _mapper;
    private readonly ILogger<MaternityLeaveSalaryService> _logger;

    public MaternityLeaveSalaryService(ApplicationDBContext dbContext, IMapper mapper, ILogger<MaternityLeaveSalaryService> logger)
    {
        _dbContext = dbContext;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<IEnumerable<MaternityLeaveSalaryDto>> GetAllMaternityLeaveSalaries()
    {
        try
        {
            var leaveSalaries = await _dbContext.MaternityLeaveSalaries.ToListAsync();
            return _mapper.Map<IEnumerable<MaternityLeaveSalaryDto>>(leaveSalaries);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching all maternity leave salaries.");
            throw;
        }
    }

    public async Task<MaternityLeaveSalaryDto?> GetMaternityLeaveSalaryById(int id)
    {
        try
        {
            var leaveSalary = await _dbContext.MaternityLeaveSalaries.FirstOrDefaultAsync(x => x.IdMaternityLeaveSalary == id);
            return leaveSalary == null ? null : _mapper.Map<MaternityLeaveSalaryDto>(leaveSalary);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching maternity leave salary with ID {Id}.", id);
            throw;
        }
    }

    public async Task<MaternityLeaveSalaryDto?> AddMaternityLeaveSalary(MaternityLeaveSalaryDto dto)
    {
        try
        {
            var entity = _mapper.Map<MaternityLeaveSalaryEntity>(dto);

            var salaryMonths = await _dbContext.SalaryMonths.FirstOrDefaultAsync(x => x.IdSalaryMonth == dto.IdSalaryMonthTo && x.IdSalaryMonth == dto.IdSalaryMonthFrom);
            if(salaryMonths == null)
            {
                return null;
            }

            var addedEntity = await _dbContext.MaternityLeaveSalaries.AddAsync(entity);
            await _dbContext.SaveChangesAsync();

            return _mapper.Map<MaternityLeaveSalaryDto>(addedEntity.Entity);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding maternity leave salary.");
            return null;
        }
    }

    public async Task<MaternityLeaveSalaryDto?> UpdateMaternityLeaveSalary(MaternityLeaveSalaryDto dto)
    {
        try
        {
            var entity = await _dbContext.MaternityLeaveSalaries.FirstOrDefaultAsync(x => x.IdMaternityLeaveSalary == dto.IdMaternityLeaveSalary);

            if (entity == null) return null;

            _mapper.Map(dto, entity);
            _dbContext.MaternityLeaveSalaries.Update(entity);
            await _dbContext.SaveChangesAsync();

            return _mapper.Map<MaternityLeaveSalaryDto>(entity);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating maternity leave salary.");
            return null;
        }
    }

    public async Task<MaternityLeaveSalaryDetailDto?> GetMaternityLeaveSalaryDetailByIdAsync(int id)
    {
        try
        {
            var result = await _dbContext.MaternityLeaveSalaryDetail.FirstOrDefaultAsync(x => x.IdMaternityLeaveSalaryDetail == id);
            return result == null ? null : _mapper.Map<MaternityLeaveSalaryDetailDto>(result);
        }
        catch(Exception ex)
        {
            _logger.LogError(ex, "Error updating maternity Leave Salary Detail.");
            return null;
        }
    }

    public async Task<MaternityLeaveSalaryDetailDto> AddMaternityLeaveSalaryDetail(MaternityLeaveSalaryDetailDto maternityLeaveSalaryDetail,int IdEmployee)
    {
        try
        {
            var entity = _mapper.Map<MaternityLeaveSalaryDetail>(maternityLeaveSalaryDetail);
            entity.CreatedDate = DateTime.Now;
            entity.CreatedBy = IdEmployee;
            var addedEntity = await _dbContext.MaternityLeaveSalaryDetail.AddAsync(entity);
            await _dbContext.SaveChangesAsync();
            return _mapper.Map<MaternityLeaveSalaryDetailDto>(addedEntity.Entity);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding maternity Leave Salary Detail.");
            return null;
        }
    }

    public async Task<MaternityLeaveSalaryDetailDto> UpdateMaternityLeaveSalaryDetail(MaternityLeaveSalaryDetailDto maternityLeaveSalaryDetail)
    {
        try
        {
            var entity = await _dbContext.MaternityLeaveSalaryDetail.FirstOrDefaultAsync(x => x.IdMaternityLeaveSalaryDetail == maternityLeaveSalaryDetail.IdMaternityLeaveSalaryDetail);

            if (entity == null) return null;

            maternityLeaveSalaryDetail.CreatedBy = entity.CreatedBy;
            maternityLeaveSalaryDetail.CreatedDate = entity.CreatedDate;
            _mapper.Map(maternityLeaveSalaryDetail, entity);
            _dbContext.MaternityLeaveSalaryDetail.Update(entity);
            await _dbContext.SaveChangesAsync();

            return _mapper.Map<MaternityLeaveSalaryDetailDto>(entity);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating maternity leave salary detail.");
            return null;
        }
    }
}
