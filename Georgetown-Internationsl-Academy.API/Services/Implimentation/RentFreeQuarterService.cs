using AutoMapper;
using Dapper;
using Georgetown_International_Academy.API.Database;
using Georgetown_Internationsl_Academy.API.DTO;
using Georgetown_Internationsl_Academy.API.Models;
using Georgetown_Internationsl_Academy.API.Services.Interface;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

public class RentFreeQuarterService : IRentFreeQuarterService
{
    private readonly ApplicationDBContext _dbContext;
    private readonly IMapper _mapper;
    private readonly ILogger<RentFreeQuarterService> _logger;

    public RentFreeQuarterService(ApplicationDBContext dbContext, IMapper mapper, ILogger<RentFreeQuarterService> logger)
    {
        _dbContext = dbContext;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<IEnumerable<RentFreeQuarterDto>> GetRentFreeQuarters(string? searchText = null, DateTime? fromDate = null)
    {
        var query = new StringBuilder(@"
            SELECT 
                rfq.IdRentFreeQuater,
                rfq.IdEmployee,
                rfq.PeriodText,
                rfq.IdRentFreeQuarterEnum,
                rfq.TotalAnnualRent,
                rfq.DurationInMonths,
                rfq.ValidFrom,
                rfq.MonthlyRent,
                rfq.ValidTo,
                rfq.TaxRate,
                rfq.AnnualTaxAmount,
                rfq.MonthlyTaxAmount,
                e.EmployeeCode,
                CONCAT(e.FirstName, ' ', COALESCE(e.MiddleName, ''), ' ', e.LastName) AS EmployeeName,
                e.IdDesignation,
                des.DesignationName,
                e.IdDepartment,
                dept.DepartmentName,
                e.JoiningDate,
                e.Gender,
                e.EmailID,
                e.PhoneNumber1,
                e.PhoneNumber2,
                e.CurrentStatus
            FROM RentFreeQuarters rfq
            LEFT JOIN Employees e ON rfq.IdEmployee = e.IdEmployee
            LEFT JOIN Designations des ON e.IdDesignation = des.IdDesignation
            LEFT JOIN Departments dept ON e.IdDepartment = dept.IdDepartment
            WHERE 1=1");

        var parameters = new DynamicParameters();

        if (!string.IsNullOrEmpty(searchText))
        {
            query.Append(@" AND (
                e.EmployeeCode LIKE @SearchText OR
                CONCAT(e.FirstName, ' ', COALESCE(e.MiddleName, ''), ' ', e.LastName) LIKE @SearchText OR
                des.DesignationName LIKE @SearchText OR
                dept.DepartmentName LIKE @SearchText
            )");
            parameters.Add("SearchText", $"%{searchText}%");
        }

        if (fromDate.HasValue)
        {
            query.Append(" AND rfq.ValidFrom >= @FromDate");
            parameters.Add("FromDate", fromDate.Value);
        }

        query.Append(" ORDER BY e.FirstName, e.LastName;");

        try
        {
            using (var connection = _dbContext.Database.GetDbConnection())
            {
                if (connection.State == System.Data.ConnectionState.Closed)
                    await connection.OpenAsync();

                var rentFreeQuarters = await connection.QueryAsync<RentFreeQuarterDto>(query.ToString(), parameters);
                return rentFreeQuarters;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching Rent-Free Quarters.");
            throw new Exception("An error occurred while fetching Rent-Free Quarters. Please try again later.");
        }
    }

    public async Task<IEnumerable<RentFreeQuarterDurationsDto>> GetRentFreeQuarterDurations()
    {
        try
        {
            var entities = await _dbContext.RentFreeQuarterDurations.ToListAsync();
            return _mapper.Map<IEnumerable<RentFreeQuarterDurationsDto>>(entities);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching rent-free quarters.");
            throw;
        }
    }

    public async Task<RentFreeQuarterDto?> GetRentFreeQuarterById(int id)
    {
        try
        {
            var entity = await _dbContext.RentFreeQuarters.FirstOrDefaultAsync(x => x.IdRentFreeQuater == id);
            if (entity == null)
            {
                _logger.LogWarning("Rent-free quarter with ID {Id} not found.", id);
                return null;
            }
            return _mapper.Map<RentFreeQuarterDto>(entity);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching rent-free quarter with ID {Id}.", id);
            throw;
        }
    }

    public async Task<RentFreeQuarterDto?> AddRentFreeQuarter(RentFreeQuarterDto dto)
    {
        try
        {           

            var templateEntity = new RentFreeQuarter();
            templateEntity.IdEmployee = dto.IdEmployee;
            templateEntity.IdRentFreeQuater = null;
            templateEntity.IdRentFreeQuarterEnum = dto.IdRentFreeQuarterEnum;
            templateEntity.TotalAnnualRent = dto.TotalAnnualRent;
            templateEntity.DurationInMonths = dto.DurationInMonths;
            templateEntity.ValidFrom = dto.ValidFrom;
            templateEntity.ValidTo = dto.ValidTo;
            templateEntity.PeriodText = dto.PeriodText;
            templateEntity.MonthlyRent = dto.MonthlyRent;
            templateEntity.TaxRate = dto.TaxRate;
            templateEntity.AnnualTaxAmount = dto.AnnualTaxAmount;
            templateEntity.MonthlyTaxAmount = dto.MonthlyTaxAmount;

            await _dbContext.RentFreeQuarters.AddAsync(templateEntity);
            await _dbContext.SaveChangesAsync();
            var resultDto = new RentFreeQuarterDto
            {
                IdRentFreeQuater = (int)templateEntity.IdRentFreeQuater,
                IdEmployee = templateEntity.IdEmployee,
                IdRentFreeQuarterEnum = templateEntity.IdRentFreeQuarterEnum,
                TotalAnnualRent = templateEntity.TotalAnnualRent,
                DurationInMonths = templateEntity.DurationInMonths,
                ValidFrom = templateEntity.ValidFrom,
                MonthlyRent = templateEntity.MonthlyRent,
                TaxRate = templateEntity.TaxRate,
                AnnualTaxAmount = templateEntity.AnnualTaxAmount,
                MonthlyTaxAmount = templateEntity.MonthlyTaxAmount
            };

            return resultDto;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding rent-free quarter: {@Dto}.", dto);
            return null;
        }
    }

    public async Task<RentFreeQuarterDto?> UpdateRentFreeQuarter(RentFreeQuarterDto dto)
    {
        try
        {
            var entity = await _dbContext.RentFreeQuarters.FirstOrDefaultAsync(x => x.IdRentFreeQuater == dto.IdRentFreeQuater);
            if (entity == null)
            {
                _logger.LogWarning("Rent-free quarter with ID {Id} not found for update.", dto.IdRentFreeQuater);
                return null;
            }

            entity.IdEmployee = dto.IdEmployee;
            entity.IdRentFreeQuarterEnum = dto.IdRentFreeQuarterEnum;
            entity.TotalAnnualRent = dto.TotalAnnualRent;
            entity.DurationInMonths = dto.DurationInMonths;
            entity.ValidFrom = dto.ValidFrom;
            entity.ValidTo = dto.ValidTo;
            entity.PeriodText = dto.PeriodText;
            entity.MonthlyRent = dto.MonthlyRent;
            entity.TaxRate = dto.TaxRate;
            entity.AnnualTaxAmount = dto.AnnualTaxAmount;
            entity.MonthlyTaxAmount = dto.MonthlyTaxAmount;

            _dbContext.RentFreeQuarters.Update(entity);
             await _dbContext.SaveChangesAsync();
            // Manually mapping the updated entity back to DTO
            var updatedDto = new RentFreeQuarterDto
            {
                IdRentFreeQuater = (int)entity.IdRentFreeQuater,
                IdEmployee = entity.IdEmployee,
                TotalAnnualRent = entity.TotalAnnualRent,
                DurationInMonths = entity.DurationInMonths,
                ValidFrom = entity.ValidFrom,
                MonthlyRent = entity.MonthlyRent,
                TaxRate = entity.TaxRate,
                AnnualTaxAmount = entity.AnnualTaxAmount,
                MonthlyTaxAmount = entity.MonthlyTaxAmount
            };

            return updatedDto;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating rent-free quarter with ID {Id}.", dto.IdRentFreeQuater);
            return null;
        }
    }

    public  async Task<IEnumerable<RentFreeQuarterAllowanceDto>> GetRentFreeQuarterAllowanceList(int? financialYear = null, string? searchString = null)
    {
        var query = new StringBuilder(@"
SELECT 
    e.IdEmployee,
    e.EmployeeCode,
    CONCAT(e.FirstName, ' ', COALESCE(e.MiddleName, ''), ' ', e.LastName) AS EmployeeName,
    d.DepartmentName,
    des.DesignationName,

    -- RFQ fields
    rfq.IdRFQ,
    rfq.IdFinancialYear,
    rfq.Duration,
    rfq.Sqft,
    rfq.Rate,

    -- Computed RFQ values
    (rfq.Duration * rfq.Sqft * rfq.Rate) AS AnnualRFQ,
    ((rfq.Duration * rfq.Sqft * rfq.Rate) / 3.0) AS TaxFree,
    ((rfq.Duration * rfq.Sqft * rfq.Rate) - ((rfq.Duration * rfq.Sqft * rfq.Rate) / 3.0)) AS TaxableAmount,
    (((rfq.Duration * rfq.Sqft * rfq.Rate) - ((rfq.Duration * rfq.Sqft * rfq.Rate) / 3.0)) * 0.40) AS TaxAmount,
    ((rfq.Duration * rfq.Sqft * rfq.Rate) - (((rfq.Duration * rfq.Sqft * rfq.Rate) - ((rfq.Duration * rfq.Sqft * rfq.Rate) / 3.0)) * 0.40)) AS NetRent,

    fy.FinancialYearName,
    fy.FinancialYearFrom,
    fy.FinancialYearTo

FROM Employees e
INNER JOIN Departments d ON e.IdDepartment = d.IdDepartment
INNER JOIN Designations des ON e.IdDesignation = des.IdDesignation
LEFT JOIN RentFreeQuarters rfq ON e.IdEmployee = rfq.IdEmployee
    " + (financialYear.HasValue ? "AND rfq.IdFinancialYear = @FinancialYear" : "") + @"
LEFT JOIN FinancialYears fy ON rfq.IdFinancialYear = fy.IdFinancialYear
WHERE e.CurrentStatus = 'WORKING'
    AND e.IdEmployee >= 1000
");

        var parameters = new DynamicParameters();

        if (financialYear.HasValue && financialYear.Value > 0)
        {
            parameters.Add("FinancialYear", financialYear.Value);
        }

        // Search filter
        if (!string.IsNullOrEmpty(searchString))
        {
            query.Append(@"
    AND (
        e.EmployeeCode LIKE @SearchText OR
        CONCAT(e.FirstName, ' ', COALESCE(e.MiddleName, ''), ' ', e.LastName) LIKE @SearchText OR
        des.DesignationName LIKE @SearchText OR
        d.DepartmentName LIKE @SearchText
    )");
            parameters.Add("SearchText", $"%{searchString}%");
        }

        query.Append(" ORDER BY e.FirstName, e.LastName;");

        try
        {
            using (var connection = _dbContext.Database.GetDbConnection())
            {
                if (connection.State == System.Data.ConnectionState.Closed)
                    await connection.OpenAsync();

                var result = await connection.QueryAsync<RentFreeQuarterAllowanceDto>(query.ToString(), parameters);
                return result;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching Rent Free Quarter Details.");
            throw new Exception("An error occurred while fetching Rent Free Quarter details.");
        }
    }
}
