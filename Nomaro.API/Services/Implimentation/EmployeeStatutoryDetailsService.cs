using AutoMapper;
using Nomaro.API.Database;
using Nomaro.API.DTO;
using Nomaro.API.Models;
using Nomaro.API.Services.Interface;
using Microsoft.EntityFrameworkCore;

namespace Nomaro.API.Services.Implimentation
{
    public class EmployeeStatutoryDetailsService : IEmployeeStatutoryDetailsService
    {
        private readonly ApplicationDBContext _dbContext;
        private readonly IMapper _mapper;
        private readonly ILogger<EmployeeStatutoryDetailsService> _logger;
        private readonly IAuditService _auditService;

        public EmployeeStatutoryDetailsService(
            ApplicationDBContext dbContext,
            IMapper mapper,
            ILogger<EmployeeStatutoryDetailsService> logger,
            IAuditService auditService)
        {
            _dbContext = dbContext;
            _mapper = mapper;
            _logger = logger;
            _auditService = auditService;
        }

        public async Task<IEnumerable<EmployeeStatutoryDetailsDto>> GetEmployeeStatutoryDetails(string? searchText)
        {
            try
            {
                var query = from s in _dbContext.EmployeeStatutoryDetails.AsNoTracking()
                            join e in _dbContext.Employees.AsNoTracking() on (int?)s.IdEmployee equals e.IdEmployee
                            select new { s, e };

                if (!string.IsNullOrWhiteSpace(searchText))
                {
                    var text = searchText.Trim();
                    query = query.Where(x =>
                        (x.e.EmployeeCode != null && x.e.EmployeeCode.Contains(text)) ||
                        x.e.FirstName.Contains(text) ||
                        (x.e.LastName != null && x.e.LastName.Contains(text)) ||
                        (x.s.PAN != null && x.s.PAN.Contains(text)) ||
                        (x.s.UAN != null && x.s.UAN.Contains(text)) ||
                        (x.s.PFNumber != null && x.s.PFNumber.Contains(text)) ||
                        (x.s.ESINumber != null && x.s.ESINumber.Contains(text)));
                }

                var rows = await query.OrderBy(x => x.e.EmployeeCode).ToListAsync();

                return rows.Select(x =>
                {
                    var dto = _mapper.Map<EmployeeStatutoryDetailsDto>(x.s);
                    dto.EmployeeCode = x.e.EmployeeCode;
                    dto.EmployeeName = FullName(x.e);
                    return dto;
                }).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving employee statutory details.");
                throw;
            }
        }

        public async Task<EmployeeStatutoryDetailsDto?> GetEmployeeStatutoryDetailsById(int idEmployee)
        {
            try
            {
                var entity = await _dbContext.EmployeeStatutoryDetails.AsNoTracking()
                    .FirstOrDefaultAsync(x => x.IdEmployee == idEmployee);
                if (entity == null)
                {
                    return null;
                }

                var dto = _mapper.Map<EmployeeStatutoryDetailsDto>(entity);
                var employee = await _dbContext.Employees.AsNoTracking()
                    .FirstOrDefaultAsync(e => e.IdEmployee == idEmployee);
                if (employee != null)
                {
                    dto.EmployeeCode = employee.EmployeeCode;
                    dto.EmployeeName = FullName(employee);
                }
                return dto;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving statutory details for employee {IdEmployee}.", idEmployee);
                throw;
            }
        }

        public async Task<bool> IsEmployeeExists(int idEmployee)
        {
            return await _dbContext.Employees.AsNoTracking().AnyAsync(e => e.IdEmployee == idEmployee);
        }

        public async Task<bool> IsStatutoryDetailsExists(int idEmployee)
        {
            return await _dbContext.EmployeeStatutoryDetails.AsNoTracking().AnyAsync(x => x.IdEmployee == idEmployee);
        }

        /// <summary>One row per employee: adds the row the first time, updates it after that.</summary>
        public async Task<bool> AddOrUpdateEmployeeStatutoryDetails(EmployeeStatutoryDetailsDto dto, int idLoggedInEmployee)
        {
            using var transaction = await _dbContext.Database.BeginTransactionAsync();
            try
            {
                var employee = await _dbContext.Employees.AsNoTracking()
                    .FirstOrDefaultAsync(e => e.IdEmployee == dto.IdEmployee);

                // Gender (M / F / O) and Date of Birth are copied from the employee when not sent
                var gender = !string.IsNullOrWhiteSpace(dto.Gender)
                    ? dto.Gender.Trim().ToUpper()
                    : (!string.IsNullOrWhiteSpace(employee?.Gender) ? employee!.Gender!.Trim().Substring(0, 1).ToUpper() : null);
                var dateOfBirth = dto.DateOfBirth ?? employee?.DateOfBirth;

                var entity = await _dbContext.EmployeeStatutoryDetails
                    .FirstOrDefaultAsync(x => x.IdEmployee == dto.IdEmployee);

                EmployeeStatutoryDetailsDto? beforeData = null;
                var isUpdate = entity != null;

                if (entity == null)
                {
                    entity = new EmployeeStatutoryDetails
                    {
                        IdEmployee = dto.IdEmployee,
                        CreatedBy = idLoggedInEmployee,
                        CreatedOn = DateTime.Now
                    };
                    await _dbContext.EmployeeStatutoryDetails.AddAsync(entity);
                }
                else
                {
                    beforeData = _mapper.Map<EmployeeStatutoryDetailsDto>(entity);
                    entity.ModifiedBy = idLoggedInEmployee;
                    entity.ModifiedOn = DateTime.Now;
                }

                entity.PAN = Clean(dto.PAN)?.ToUpper();
                entity.UAN = Clean(dto.UAN);
                entity.PFNumber = Clean(dto.PFNumber)?.ToUpper();
                entity.IsPFApplicable = dto.IsPFApplicable;
                entity.PFOnActualWage = dto.PFOnActualWage;
                entity.IsEPSApplicable = dto.IsEPSApplicable;
                entity.VPFRate = dto.VPFRate;
                entity.ESINumber = Clean(dto.ESINumber);
                entity.IsESIApplicable = dto.IsESIApplicable;
                entity.IsDisabled = dto.IsDisabled;
                entity.IdPTState = dto.IdPTState;
                entity.IsPTApplicable = dto.IsPTApplicable;
                entity.IsLWFApplicable = dto.IsLWFApplicable;
                entity.Gender = gender;
                entity.DateOfBirth = dateOfBirth;

                await _dbContext.SaveChangesAsync();
                await transaction.CommitAsync();

                await _auditService.LogAuditAsync(
                    actionType: isUpdate ? "Update" : "Create",
                    entityName: "EmployeeStatutoryDetails",
                    entityId: entity.IdEmployee,
                    actionDetails: isUpdate
                        ? new { before = beforeData, after = _mapper.Map<EmployeeStatutoryDetailsDto>(entity) }
                        : _mapper.Map<EmployeeStatutoryDetailsDto>(entity));

                return true;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error saving statutory details for employee {IdEmployee}.", dto.IdEmployee);
                return false;
            }
        }

        public async Task<IEnumerable<StateDto>> GetStates(bool? hasPT, bool? hasLWF, bool? isActive)
        {
            try
            {
                var query = _dbContext.States.AsNoTracking();

                if (hasPT.HasValue)
                {
                    query = query.Where(x => x.HasPT == hasPT.Value);
                }
                if (hasLWF.HasValue)
                {
                    query = query.Where(x => x.HasLWF == hasLWF.Value);
                }
                if (isActive.HasValue)
                {
                    query = query.Where(x => x.IsActive == isActive.Value);
                }

                var states = await query.OrderBy(x => x.StateName).ToListAsync();
                return _mapper.Map<List<StateDto>>(states);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving states.");
                throw;
            }
        }

        private static string? Clean(string? value) =>
            string.IsNullOrWhiteSpace(value) ? null : value.Trim();

        private static string FullName(Employee e) =>
            string.Join(" ", new[] { e.FirstName, e.MiddleName, e.LastName }.Where(n => !string.IsNullOrWhiteSpace(n)));
    }
}
