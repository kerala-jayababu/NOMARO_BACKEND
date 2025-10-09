using AutoMapper;
using Dapper;
using Georgetown_International_Academy.API.Database;
using Georgetown_Internationsl_Academy.API.DTO;
using Georgetown_Internationsl_Academy.API.Models;
using Georgetown_Internationsl_Academy.API.Models.YourNamespace.Models;
using Georgetown_Internationsl_Academy.API.Services.Interface;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using static Azure.Core.HttpHeader;

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

    public async Task<IEnumerable<RentFreeQuarterAllowanceDto>> GetRentFreeQuarterAllowanceList(int? financialYear = null, string? searchString = null)
    {
        var query = new StringBuilder(@"
SELECT 
    r.IdRentFreeQuarterAllowance,
    r.IdEmployee,
    e.EmployeeCode,
    CONCAT(e.FirstName, ' ', COALESCE(e.MiddleName, ''), ' ', e.LastName) AS EmployeeName,
    d.DepartmentName,
    des.DesignationName,
    r.Duration,
    r.FinancialYear,
    r.AllottedSqft,
    r.SqFtRate,
    r.TaxRate,
    r.AnnualRFQAllowance,
    r.TaxFreeAllowance,
    r.TaxableAmount,
    r.TaxAmount,
    r.NetRFQAllowance,
    fy.FinancialYearName,
    r.CreatedDate,
    r.UpdatedDate,
    r.CreatedBy
FROM RentFreeQuarterAllowance r
INNER JOIN Employees e ON r.IdEmployee = e.IdEmployee
INNER JOIN Departments d ON e.IdDepartment = d.IdDepartment
INNER JOIN Designations des ON e.IdDesignation = des.IdDesignation
LEFT JOIN FinancialYears fy ON r.FinancialYear = fy.IdFinancialYear
WHERE e.CurrentStatus = 'WORKING'
");

        var parameters = new DynamicParameters();

        if (financialYear.HasValue && financialYear.Value > 0)
        {
            query.Append(" AND r.FinancialYear = @FinancialYear");
            parameters.Add("FinancialYear", financialYear.Value);
        }

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
            _logger.LogError(ex, "Error fetching Rent Free Quarter Allowance details.");
            throw new Exception("An error occurred while fetching Rent Free Quarter Allowance details.");
        }
    }

    public async Task<RentFreeQuarterAllowanceDto?> GetRentFreeQuarterAllowanceById(int id)
    {
        var query = new StringBuilder(@"
SELECT 
    r.IdRentFreeQuarterAllowance,
    r.IdEmployee,
    e.EmployeeCode,
    CONCAT(e.FirstName, ' ', COALESCE(e.MiddleName, ''), ' ', e.LastName) AS EmployeeName,
    d.DepartmentName,
    des.DesignationName,
    r.Duration,
    r.FinancialYear,
    r.AllottedSqft,
    r.SqFtRate,
    r.TaxRate,
    r.AnnualRFQAllowance,
    r.TaxFreeAllowance,
    r.TaxableAmount,
    r.TaxAmount,
    r.NetRFQAllowance,
    fy.FinancialYearName,
    r.CreatedDate,
    r.UpdatedDate,
    r.CreatedBy
FROM RentFreeQuarterAllowance r
INNER JOIN Employees e ON r.IdEmployee = e.IdEmployee
INNER JOIN Departments d ON e.IdDepartment = d.IdDepartment
INNER JOIN Designations des ON e.IdDesignation = des.IdDesignation
LEFT JOIN FinancialYears fy ON r.FinancialYear = fy.IdFinancialYear
WHERE e.CurrentStatus = 'WORKING'
  AND r.IdRentFreeQuarterAllowance = @Id
");

        var parameters = new DynamicParameters();
        parameters.Add("Id", id);

        try
        {
            using (var connection = _dbContext.Database.GetDbConnection())
            {
                if (connection.State == System.Data.ConnectionState.Closed)
                    await connection.OpenAsync();

                var result = await connection.QueryFirstOrDefaultAsync<RentFreeQuarterAllowanceDto>(query.ToString(), parameters);
                return result;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error fetching Rent Free Quarter Allowance details for Id: {id}");
            throw new Exception("An error occurred while fetching Rent Free Quarter Allowance details by Id.");
        }
    }

    public async Task<RentFreeQuarterAllowanceAddOrUpdateDto?> AddRentFreeQuarterAllowance(RentFreeQuarterAllowanceAddOrUpdateDto dto, int IdEmployee)
    {
        try
        {
            var entity = new RentFreeQuarterAllowance
            {
                IdEmployee = dto.IdEmployee,
                Duration = dto.Duration,
                FinancialYear = dto.FinancialYear,
                AllottedSqft = dto.AllottedSqFt,
                SqFtRate = dto.SqFtRate,
                AnnualRFQAllowance = dto.AnnualRFQAllowance,
                TaxFreeAllowance = dto.TaxFreeAllowance,
                TaxableAmount = dto.TaxableAmount,
                TaxAmount = dto.TaxAmount,
                TaxRate = dto.TaxRate,
                NetRFQAllowance = dto.NetRFQAllowance,
                CreatedDate = DateTime.Now,
                CreatedBy = IdEmployee
            };

            await _dbContext.RentFreeQuarterAllowance.AddAsync(entity);
            await _dbContext.SaveChangesAsync();

            dto.IdRentFreeQuarterAllowance = entity.IdRentFreeQuarterAllowance;
            return dto;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding RentFreeQuarterAllowance: {@Dto}", dto);
            return null;
        }
    }


    public async Task<RentFreeQuarterAllowanceAddOrUpdateDto?> UpdateRentFreeQuarterAllowance(RentFreeQuarterAllowanceAddOrUpdateDto dto, int IdEmployee)
    {
        try
        {
            var entity = await _dbContext.RentFreeQuarterAllowance
                .FirstOrDefaultAsync(x => x.IdRentFreeQuarterAllowance == dto.IdRentFreeQuarterAllowance);

            if (entity == null)
            {
                _logger.LogWarning("RentFreeQuarterAllowance with ID {Id} not found for update.", dto.IdRentFreeQuarterAllowance);
                return null;
            }

            entity.IdEmployee = dto.IdEmployee;
            entity.Duration = dto.Duration;
            entity.FinancialYear = dto.FinancialYear;
            entity.AllottedSqft = dto.AllottedSqFt;
            entity.SqFtRate = dto.SqFtRate;
            entity.TaxRate = dto.TaxRate;
            entity.AnnualRFQAllowance = dto.AnnualRFQAllowance;
            entity.TaxFreeAllowance = dto.TaxFreeAllowance;
            entity.TaxableAmount = dto.TaxableAmount;
            entity.TaxAmount = dto.TaxAmount;
            entity.NetRFQAllowance = dto.NetRFQAllowance;
            entity.UpdatedDate = DateTime.Now;
            _dbContext.RentFreeQuarterAllowance.Update(entity);
            await _dbContext.SaveChangesAsync();

            return dto;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating RentFreeQuarterAllowance with ID {Id}", dto.IdRentFreeQuarterAllowance);
            return null;
        }
    }


}
