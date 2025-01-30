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
}
