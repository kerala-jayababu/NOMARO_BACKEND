using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Nomaro.API.Database;
using Nomaro.API.DTO;
using Nomaro.API.Models;
using Nomaro.API.Services.Interface;

namespace Nomaro.API.Services.Implimentation
{
    public class GradeService : IGradeService
    {
        private readonly ApplicationDBContext _dbContext;
        private readonly IMapper _mapper;
        private readonly ILogger<GradeService> _logger;
        private readonly IAuditService _auditService;

        public GradeService(
            ApplicationDBContext dbContext,
            IMapper mapper,
            ILogger<GradeService> logger,
            IAuditService auditService)
        {
            _dbContext = dbContext;
            _mapper = mapper;
            _logger = logger;
            _auditService = auditService;
        }

        public async Task<IEnumerable<GradeDto>> GetGrades(bool? isActive)
        {
            try
            {
                var query = _dbContext.Grades.AsNoTracking();

                if (isActive.HasValue)
                {
                    query = query.Where(x => x.IsActive == isActive.Value);
                }

                return await query
                    .OrderBy(x => x.GradeLevel)
                    .ThenBy(x => x.GradeCode)
                    .Select(x => new GradeDto
                    {
                        IdGrade = x.IdGrade,
                        GradeCode = x.GradeCode,
                        GradeName = x.GradeName,
                        GradeLevel = x.GradeLevel,
                        Description = x.Description,
                        IsActive = x.IsActive,
                        EmployeeCount = _dbContext.Employees.Count(e => e.IdGrade == x.IdGrade && e.CurrentStatus == "Working")
                    })
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching Grades.");
                throw;
            }
        }

        public async Task<GradeDto?> GetGradeById(int idGrade)
        {
            var entity = await _dbContext.Grades.AsNoTracking().FirstOrDefaultAsync(x => x.IdGrade == idGrade);
            return entity == null ? null : _mapper.Map<GradeDto>(entity);
        }

        public async Task<bool> IsGradeCodeExists(string gradeCode, int idGrade)
        {
            var code = gradeCode.Trim();
            return await _dbContext.Grades.AnyAsync(x => x.GradeCode == code && x.IdGrade != idGrade);
        }

        public async Task<bool> AddOrUpdateGrade(GradeDto dto, int idLoggedInEmployee)
        {
            try
            {
                var entity = await _dbContext.Grades.FirstOrDefaultAsync(x => x.IdGrade == dto.IdGrade);

                GradeDto? beforeData = null;
                var isUpdate = entity != null;

                if (entity != null)
                {
                    beforeData = _mapper.Map<GradeDto>(entity);
                    entity.GradeCode = dto.GradeCode.Trim().ToUpper();
                    entity.GradeName = dto.GradeName.Trim();
                    entity.GradeLevel = dto.GradeLevel;
                    entity.Description = string.IsNullOrWhiteSpace(dto.Description) ? null : dto.Description.Trim();
                    entity.IsActive = dto.IsActive;
                    entity.IdModifiedBy = idLoggedInEmployee;
                    entity.ModifiedDateTime = DateTime.Now;
                }
                else
                {
                    entity = new Grades
                    {
                        GradeCode = dto.GradeCode.Trim().ToUpper(),
                        GradeName = dto.GradeName.Trim(),
                        GradeLevel = dto.GradeLevel,
                        Description = string.IsNullOrWhiteSpace(dto.Description) ? null : dto.Description.Trim(),
                        IsActive = dto.IsActive,
                        IdCreatedBy = idLoggedInEmployee,
                        CreatedDateTime = DateTime.Now
                    };
                    await _dbContext.Grades.AddAsync(entity);
                }

                await _dbContext.SaveChangesAsync();

                await _auditService.LogAuditAsync(
                    actionType: isUpdate ? "Update" : "Create",
                    entityName: "Grade",
                    entityId: entity.IdGrade,
                    actionDetails: isUpdate
                        ? new { before = beforeData, after = _mapper.Map<GradeDto>(entity) }
                        : (object)new { after = _mapper.Map<GradeDto>(entity) });

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding or updating Grade {@Grade}", dto);
                throw;
            }
        }
    }
}
