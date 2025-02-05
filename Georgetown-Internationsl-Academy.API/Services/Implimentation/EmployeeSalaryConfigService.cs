using AutoMapper;
using Dapper;
using Georgetown_International_Academy.API.Database;
using Georgetown_Internationsl_Academy.API.DTO;
using Georgetown_Internationsl_Academy.API.Models;
using Georgetown_Internationsl_Academy.API.Services.Interface;
using Microsoft.EntityFrameworkCore;
using System.Text;

namespace Georgetown_Internationsl_Academy.API.Services.Implimentation
{
    public class EmployeeSalaryConfigService : IEmployeeSalaryConfigService
    {
        private readonly ApplicationDBContext _dbContext;
        private readonly IMapper _mapper;
        private readonly ILogger<EmployeeSalaryConfigService> _logger;

        public EmployeeSalaryConfigService(ApplicationDBContext dbContext, IMapper mapper, ILogger<EmployeeSalaryConfigService> logger)
        {
            _dbContext = dbContext;
            _mapper = mapper;
            _logger = logger;
        }

        #region EmployeeSalaryConfig
        public async Task<IEnumerable<EmployeeSalaryConfigDto>> GetAllConfigs(string? searchText = null, DateTime? dateFilter = null, string? dropdownFilter = null)
        {
            var query = new StringBuilder(@"
         SELECT 
      esc.IdEmployeeSalaryConfig,
	  esc.ValidFrom,
	  esc.ValidTo,
	  esc.IdSalaryTemplate,
	  esc.ActiveStatus,
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
      esc. TotalEarnings,
    esc.TotalDeductions,
      esc.NetSalary,
      esc.ApprovalStatus
  FROM EmployeeSalaryConfig esc
  INNER JOIN Employees e ON esc.IdEmployee = e.IdEmployee
  INNER JOIN Departments d ON e.IdDepartment = d.IdDepartment
  INNER JOIN Designations des ON e.IdDesignation = des.IdDesignation
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
    esc.CreatedOn >= @DateFilter 
) ");

               


                parameters.Add("DateFilter", dateFilter.Value.Date);
            }

            // Apply dropdown filter if provided (ApprovalStatus)
            if (!string.IsNullOrEmpty(dropdownFilter))
            {
                query.Append(" AND esc.ApprovalStatus = @DropdownFilter ");
                parameters.Add("DropdownFilter", dropdownFilter);

                // If dropdownFilter is "Approved", also append ValidTo IS NULL
                if (dropdownFilter.Equals("Approved", StringComparison.OrdinalIgnoreCase))
                {
                    query.Append(" AND esc.ValidTo IS NULL ");
                }
            }

            query.Append(" ORDER BY e.FirstName, e.LastName;");

            try
            {
                using (var connection = _dbContext.Database.GetDbConnection())
                {
                    if (connection.State == System.Data.ConnectionState.Closed)
                        await connection.OpenAsync();

                    var configs = await connection.QueryAsync<EmployeeSalaryConfigDto>(query.ToString(), parameters);
                    return configs;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching Employee Salary Configurations.");
                throw new Exception("An error occurred while fetching configurations. Please try again later.");
            }
        }

        public async Task<EmployeeSalaryConfigDto?> GetConfigById(int id)
        {
            try
            {
                var config = await _dbContext.EmployeeSalaryConfig.FindAsync(id);
                return config == null ? null : _mapper.Map<EmployeeSalaryConfigDto>(config);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error fetching Employee Salary Configuration with ID {id}.");
                throw new Exception("An error occurred while fetching the configuration. Please try again later.");
            }
        }

        public async Task<EmployeeSalaryConfigDto?> AddConfig(EmployeeSalaryConfigDto dto,int IdEmployee)
        {
            try
            {
                var entity = _mapper.Map<EmployeeSalaryConfig>(dto);
                entity.CreatedBy = IdEmployee;
                entity.CreatedOn = DateTime.Now;
                _dbContext.EmployeeSalaryConfig.Add(entity);
                await _dbContext.SaveChangesAsync();
                return _mapper.Map<EmployeeSalaryConfigDto>(entity);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding new Employee Salary Configuration.");
                throw new Exception("An error occurred while adding the configuration. Please try again later.");
            }
        }

        public async Task<EmployeeSalaryConfigDto?> UpdateConfig(EmployeeSalaryConfigDto dto)
        {
            try
            {
                var config = await _dbContext.EmployeeSalaryConfig.FirstOrDefaultAsync(c => c.IdEmployeeSalaryConfig == dto.IdEmployeeSalaryConfig);
                if (config == null)
                {
                    _logger.LogWarning("Attempt to update a non-existent employee salary configuration with ID: {Id}.", dto.IdEmployeeSalaryConfig);
                    return null;
                }

                // Manual mapping
                config.IdEmployee = dto.IdEmployee;
                config.ValidFrom = dto.ValidFrom;
                config.ValidTo = dto.ValidTo;
                config.IdSalaryTemplate = dto.IdSalaryTemplate;               
                config.TotalDeductions = dto.TotalDeductions;
                config.TotalEarnings = dto.TotalEarnings;
                config.NetSalary = dto.NetSalary;
                config.ApprovalStatus = dto.ApprovalStatus;
                config.ActiveStatus = dto.ActiveStatus;

                _dbContext.EmployeeSalaryConfig.Update(config);
                await _dbContext.SaveChangesAsync();

                return _mapper.Map<EmployeeSalaryConfigDto>(config);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating employee salary configuration with ID: {dto.IdEmployeeSalaryConfig}");
                return null;
            }
        }


        #endregion

        #region EmployeeSalaryConfigDetails

        public async Task<IEnumerable<EmployeeSalaryConfigDetailsDto>> GetAllDetails(int Id)
        {
            try
            {
                var details = await _dbContext.EmployeeSalaryConfigDetails.Where(x=>x.IdEmployeeSalaryConfig == Id) .ToListAsync();
                return _mapper.Map<IEnumerable<EmployeeSalaryConfigDetailsDto>>(details);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching Employee Salary Configuration Details.");
                throw new Exception("An error occurred while fetching details. Please try again later.");
            }
        }

        public async Task<EmployeeSalaryConfigDetailsDto?> GetDetailById(int id)
        {
            try
            {
                var detail = await _dbContext.EmployeeSalaryConfigDetails.FindAsync(id);
                return detail == null ? null : _mapper.Map<EmployeeSalaryConfigDetailsDto>(detail);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error fetching Employee Salary Configuration Detail with ID {id}.");
                throw new Exception("An error occurred while fetching the detail. Please try again later.");
            }
        }

        public async Task<EmployeeSalaryConfigDetailsDto?> AddDetail(EmployeeSalaryConfigDetailsDto dto)
        {
            try
            {
                var entity = _mapper.Map<EmployeeSalaryConfigDetails>(dto);
                _dbContext.EmployeeSalaryConfigDetails.Add(entity);
                await _dbContext.SaveChangesAsync();
                return _mapper.Map<EmployeeSalaryConfigDetailsDto>(entity);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding new Employee Salary Configuration Detail.");
                throw new Exception("An error occurred while adding the detail. Please try again later.");
            }
        }

        public async Task<EmployeeSalaryConfigDetailsDto?> UpdateDetail(EmployeeSalaryConfigDetailsDto dto)
        {
            try
            {
                var existingDetail = await _dbContext.EmployeeSalaryConfigDetails.FirstOrDefaultAsync(d => d.IdEmployeeSalaryConfigDetail == dto.IdEmployeeSalaryConfigDetail);
                if (existingDetail == null)
                {
                    _logger.LogWarning("Attempt to update a non-existent employee salary config detail with ID: {Id}.", dto.IdEmployeeSalaryConfigDetail);
                    return null;
                }

                // Manual mapping
                existingDetail.IdEmployeeSalaryConfig = dto.IdEmployeeSalaryConfig;
                existingDetail.IdSalaryHead = dto.IdSalaryHead;
                existingDetail.CalculationMethod = dto.CalculationMethod;
                existingDetail.FixedAmount = dto.FixedAmount;
                existingDetail.PercentageValue = dto.PercentageValue;
                existingDetail.CustomFormula = dto.CustomFormula;

                _dbContext.EmployeeSalaryConfigDetails.Update(existingDetail);
                await _dbContext.SaveChangesAsync();

                return _mapper.Map<EmployeeSalaryConfigDetailsDto>(existingDetail);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating employee salary config detail with ID: {dto.IdEmployeeSalaryConfigDetail}");
                return null;
            }
        }


        #endregion
    }
}
