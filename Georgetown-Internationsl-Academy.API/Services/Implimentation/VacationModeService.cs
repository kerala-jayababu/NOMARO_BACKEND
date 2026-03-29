using AutoMapper;
using Dapper;
using Georgetown_International_Academy.API.Database;
using Georgetown_Internationsl_Academy.API.DTO;
using Georgetown_Internationsl_Academy.API.Models;
using Georgetown_Internationsl_Academy.API.Services.Interface;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text;

public class VacationModeService : IVacationModeService
{
    private readonly ApplicationDBContext _dbContext;
    private readonly IMapper _mapper;
    private readonly ILogger<VacationModeService> _logger;
    private readonly IAuditService _auditService;

    public VacationModeService(ApplicationDBContext dbContext, IMapper mapper, ILogger<VacationModeService> logger, IAuditService auditService)
    {
        _dbContext = dbContext;
        _mapper = mapper;
        _logger = logger;
        _auditService = auditService;
    }

    public async Task<IEnumerable<VacationModeDto>> GetAllVacationModes(string? searchText = null, DateTime? dateFilter = null)
    {
        var query = new StringBuilder(@"
        SELECT 
 vm.IdVacationMode,
            e.IdEmployee,
            e.EmployeeCode,
            CONCAT(e.FirstName, ' ', COALESCE(e.MiddleName, ''), ' ', e.LastName) AS EmployeeName,
            e.IdDesignation,
            des.DesignationName,
            e.IdDepartment,
            d.DepartmentName,
            e.JoiningDate,
            e.Gender,
            e.EmailID,
            e.PhoneNumber1, 
            e.PhoneNumber2,
            e.CurrentStatus,
            vm.VacationFrom ,
            vm.VacationTo ,
            vm.IdSubstitueEmployee,
            CONCAT(se.FirstName, ' ', COALESCE(se.MiddleName, ''), ' ', se.LastName) AS SubstituteEmployeeName,
            vm.ReasonForVacation
        FROM VacationModes vm
        INNER JOIN Employees e ON vm.IdEmployee = e.IdEmployee
        INNER JOIN Departments d ON e.IdDepartment = d.IdDepartment
        INNER JOIN Designations des ON e.IdDesignation = des.IdDesignation
        LEFT JOIN Employees se ON vm.IdSubstitueEmployee = se.IdEmployee
        WHERE 1=1 ");

        var parameters = new DynamicParameters();

        // Apply search filter if provided
        if (!string.IsNullOrEmpty(searchText))
        {
            query.Append(@"
AND (
    e.EmployeeCode LIKE @SearchText
    OR CONCAT(e.FirstName, ' ', COALESCE(e.MiddleName, ''), ' ', e.LastName) LIKE @SearchText
    OR des.DesignationName LIKE @SearchText
    OR d.DepartmentName LIKE @SearchText
) ");
            parameters.Add("SearchText", $"%{searchText}%");
        }

        // Apply date filter if provided
        if (dateFilter.HasValue)
        {
            query.Append(@"
AND (
    vm.VacationFrom  >= @DateFilter
) ");
            parameters.Add("DateFilter", dateFilter.Value.Date);
        }

        query.Append(" ORDER BY e.FirstName, e.LastName;");

        try
        {
            _logger.LogInformation("Fetching Vacation Employee list with filters using Dapper.");

            using (var connection = _dbContext.Database.GetDbConnection())
            {
                if (connection.State == System.Data.ConnectionState.Closed)
                    await connection.OpenAsync();

                var vacationEmployees = await connection.QueryAsync<VacationModeDto>(query.ToString(), parameters);
                return vacationEmployees;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching the Vacation Employee list using Dapper.");
            throw;
        }
    }


    public async Task<VacationModeDto?> GetVacationModeById(int id)
    {
        try
        {
            var vacationMode = await _dbContext.VacationModes.FirstOrDefaultAsync(vm => vm.IdVacationMode == id);
            return vacationMode == null ? null : _mapper.Map<VacationModeDto>(vacationMode);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching vacation mode with ID: {Id}", id);
            throw;
        }
    }

    public async Task<VacationModeDto?> AddVacationMode(VacationModeDto vacationModeDto)
    {
        try
        {
            var vacationModeEntity = _mapper.Map<VacationMode>(vacationModeDto);
            var addedEntity = await _dbContext.VacationModes.AddAsync(vacationModeEntity);
            await _dbContext.SaveChangesAsync();

            await _auditService.LogAuditAsync(
                actionType: "Create",
                entityName: "VacationMode",
                entityId: addedEntity.Entity.IdVacationMode,
                actionDetails: new { after = addedEntity.Entity });

            return _mapper.Map<VacationModeDto>(addedEntity.Entity);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding vacation mode: {@VacationModeDto}", vacationModeDto);
            return null;
        }
    }

    public async Task<VacationModeDto?> UpdateVacationMode(VacationModeDto vacationModeDto)
    {
        try
        {
            var vacationMode = await _dbContext.VacationModes.FirstOrDefaultAsync(vm => vm.IdVacationMode == vacationModeDto.IdVacationMode);

            if (vacationMode == null)
            {
                _logger.LogWarning("Vacation mode with ID {Id} not found.", vacationModeDto.IdVacationMode);
                return null;
            }

            var beforeUpdate = _mapper.Map<VacationModeDto>(vacationMode);

            vacationMode.IdEmployee = vacationModeDto.IdEmployee;
            vacationMode.VacationFrom = vacationModeDto.VacationFrom;
            vacationMode.VacationTo = vacationModeDto.VacationTo;
            vacationMode.IdSubstitueEmployee = vacationModeDto.IdSubstitueEmployee;
            vacationMode.ReasonForVacation = vacationModeDto.ReasonForVacation;

            var updatedEntity = _dbContext.VacationModes.Update(vacationMode);
            await _dbContext.SaveChangesAsync();

            await _auditService.LogAuditAsync(
                actionType: "Update",
                entityName: "VacationMode",
                entityId: updatedEntity.Entity.IdVacationMode,
                actionDetails: new { before = beforeUpdate, after = _mapper.Map<VacationModeDto>(updatedEntity.Entity) });

            return _mapper.Map<VacationModeDto>(updatedEntity.Entity);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating vacation mode with ID: {Id}", vacationModeDto.IdVacationMode);
            return null;
        }
    }
}
