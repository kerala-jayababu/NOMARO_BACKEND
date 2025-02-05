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
        private readonly IConfiguration _configuration;

        public EmployeeSalaryConfigService(ApplicationDBContext dbContext, IMapper mapper, ILogger<EmployeeSalaryConfigService> logger, IConfiguration configuration)
        {
            _dbContext = dbContext;
            _mapper = mapper;
            _logger = logger;
            _configuration = configuration;
        }

        #region EmployeeSalaryConfig
        public async Task<IEnumerable<EmployeeSalaryConfigDto>> GetAllConfigs(string? searchText = null, string? dropdownFilter = null)
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
        esc.TotalEarnings,
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

            // Handle dropdown filter logic
            if (!string.IsNullOrEmpty(dropdownFilter))
            {
                if (dropdownFilter.Equals("SUBMITTED", StringComparison.OrdinalIgnoreCase))
                {
                    // Include SUBMITTED and Interim Approved statuses
                    query.Append(@"
            AND (
                esc.ApprovalStatus = 'SUBMITTED'
                OR esc.ApprovalStatus = 'INTERIM APPROVED'
            )");
                }
                else if (dropdownFilter.Equals("APPROVED", StringComparison.OrdinalIgnoreCase))
                {
                    // Include only the record with MAX(ValidFrom) for APPROVED status
                    query.Append(@"
            AND esc.ApprovalStatus = 'APPROVED'
            AND esc.ValidFrom = (
                SELECT MAX(ValidFrom)
                FROM EmployeeSalaryConfig
                WHERE IdEmployee = esc.IdEmployee
                AND ApprovalStatus = 'APPROVED'
            )");
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
                // Fetch EmployeeSalaryConfig by ID
                var config = await _dbContext.EmployeeSalaryConfig.FirstOrDefaultAsync(c => c.IdEmployeeSalaryConfig == id);
                if (config == null)
                {
                    return null; // Return null if config is not found
                }

                // Map the EmployeeSalaryConfig to DTO
                var configDto = _mapper.Map<EmployeeSalaryConfigDto>(config);

                // Fetch associated EmployeeSalaryConfigDetails
                var details = await _dbContext.EmployeeSalaryConfigDetails
                    .Where(d => d.IdEmployeeSalaryConfig == config.IdEmployeeSalaryConfig)
                    .ToListAsync();

                // Map the details to DTO and attach to config DTO
                configDto.EmployeeSalaryConfigDetails = _mapper.Map<List<EmployeeSalaryConfigDetailsDto>>(details);

                return configDto;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error fetching Employee Salary Configuration with ID {id}.");
                throw new Exception("An error occurred while fetching the configuration. Please try again later.");
            }
        }


        public async Task<EmployeeSalaryConfigDto?> AddConfig(EmployeeSalaryConfigDto dto, int IdEmployee)
        {
            using var transaction = await _dbContext.Database.BeginTransactionAsync();

            try
            {
                // Map and insert EmployeeSalaryConfig
                var configEntity = _mapper.Map<EmployeeSalaryConfig>(dto);
                configEntity.CreatedBy = IdEmployee;
                configEntity.CreatedOn = DateTime.Now;

                _dbContext.EmployeeSalaryConfig.Add(configEntity);
                await _dbContext.SaveChangesAsync();

                // Insert related EmployeeSalaryConfigDetails
                if (dto.EmployeeSalaryConfigDetails != null && dto.EmployeeSalaryConfigDetails.Any())
                {
                    foreach (var detailDto in dto.EmployeeSalaryConfigDetails)
                    {
                        var detailEntity = _mapper.Map<EmployeeSalaryConfigDetails>(detailDto);
                        detailEntity.IdEmployeeSalaryConfig = configEntity.IdEmployeeSalaryConfig;

                        _dbContext.EmployeeSalaryConfigDetails.Add(detailEntity);
                    }

                    await _dbContext.SaveChangesAsync();
                }

                await transaction.CommitAsync();
                return _mapper.Map<EmployeeSalaryConfigDto>(configEntity);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error adding Employee Salary Configuration and Details.");
                throw new Exception("An error occurred while adding the configuration. Please try again later.");
            }
        }


        public async Task<EmployeeSalaryConfigDto?> UpdateConfig(EmployeeSalaryConfigDto dto)
        {
            using var transaction = await _dbContext.Database.BeginTransactionAsync();

            try
            {
                // Update EmployeeSalaryConfig
                var configEntity = await _dbContext.EmployeeSalaryConfig.FirstOrDefaultAsync(c => c.IdEmployeeSalaryConfig == dto.IdEmployeeSalaryConfig);
                if (configEntity == null)
                {
                    _logger.LogWarning("Attempt to update a non-existent Employee Salary Configuration with ID: {Id}.", dto.IdEmployeeSalaryConfig);
                    return null;
                }

                // Manual mapping for update
                configEntity.IdEmployee = dto.IdEmployee;
                configEntity.ValidFrom = dto.ValidFrom;
                configEntity.ValidTo = dto.ValidTo;
                configEntity.IdSalaryTemplate = dto.IdSalaryTemplate;
                configEntity.TotalEarnings = dto.TotalEarnings;
                configEntity.TotalDeductions = dto.TotalDeductions;
                configEntity.NetSalary = dto.NetSalary;
                configEntity.ApprovalStatus = dto.ApprovalStatus;
                configEntity.ActiveStatus = dto.ActiveStatus;

                _dbContext.EmployeeSalaryConfig.Update(configEntity);
                await _dbContext.SaveChangesAsync();

                // Update EmployeeSalaryConfigDetails
                if (dto.EmployeeSalaryConfigDetails != null)
                {
                    var existingDetails = await _dbContext.EmployeeSalaryConfigDetails
                        .Where(d => d.IdEmployeeSalaryConfig == configEntity.IdEmployeeSalaryConfig)
                        .ToListAsync();

                    // Delete details that are no longer in the DTO
                    var detailsToDelete = existingDetails
                        .Where(d => !dto.EmployeeSalaryConfigDetails.Any(dtoDetail => dtoDetail.IdEmployeeSalaryConfigDetail == d.IdEmployeeSalaryConfigDetail))
                        .ToList();
                    _dbContext.EmployeeSalaryConfigDetails.RemoveRange(detailsToDelete);

                    // Update existing details
                    foreach (var detailDto in dto.EmployeeSalaryConfigDetails)
                    {
                        var existingDetail = existingDetails.FirstOrDefault(d => d.IdEmployeeSalaryConfigDetail == detailDto.IdEmployeeSalaryConfigDetail);
                        if (existingDetail != null)
                        {
                            existingDetail.IdSalaryHead = detailDto.IdSalaryHead;
                            existingDetail.CalculationMethod = detailDto.CalculationMethod;
                            existingDetail.FixedAmount = detailDto.FixedAmount;
                            existingDetail.PercentageValue = detailDto.PercentageValue;
                            existingDetail.CustomFormula = detailDto.CustomFormula;

                            _dbContext.EmployeeSalaryConfigDetails.Update(existingDetail);
                        }
                        else
                        {
                            // Add new details
                            var newDetailEntity = _mapper.Map<EmployeeSalaryConfigDetails>(detailDto);
                            newDetailEntity.IdEmployeeSalaryConfig = configEntity.IdEmployeeSalaryConfig;

                            _dbContext.EmployeeSalaryConfigDetails.Add(newDetailEntity);
                        }
                    }

                    await _dbContext.SaveChangesAsync();
                }

                await transaction.CommitAsync();
                return _mapper.Map<EmployeeSalaryConfigDto>(configEntity);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error updating Employee Salary Configuration and Details.");
                throw new Exception("An error occurred while updating the configuration. Please try again later.");
            }
        }


        #endregion


    }
}
