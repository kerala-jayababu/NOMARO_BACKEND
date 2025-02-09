using AutoMapper;
using Dapper;
using Georgetown_International_Academy.API.Database;
using Georgetown_Internationsl_Academy.API.DTO;
using Georgetown_Internationsl_Academy.API.Models;
using Georgetown_Internationsl_Academy.API.Services.Interface;
using Microsoft.EntityFrameworkCore;
using System.Text;

public class ScheduledSalaryDeductionService : IScheduledSalaryDeductionService
{
    private readonly ApplicationDBContext _dbContext;
    private readonly IMapper _mapper;
    private readonly ILogger<ScheduledSalaryDeductionService> _logger;

    public ScheduledSalaryDeductionService(ApplicationDBContext dbContext, IMapper mapper, ILogger<ScheduledSalaryDeductionService> logger)
    {
        _dbContext = dbContext;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<IEnumerable<ScheduledSalaryDeductionDto>> GetScheduledDeductions(string? searchText = null, DateTime? fromDate = null)
    {
        var query = new StringBuilder(@"
    SELECT 
        ssd.IdScheduledSalaryDeduction,
        ssd.IdEmployee,
        ssd.TotalAmount,
        ssd.DeductionFromSalaryMonth,
        ssd.DeductionToSalaryMonth,
        ssd.DeductionFromSalaryMonthDate,
        ssd.DeductionToSalaryMonthDate,
        ssd.AllocatingSalaryHead,
        ssd.MonthCount,
        ssd.MonthlyDeductableAmount,
        smFrom.SalaryMonthText AS DeductionFromSalaryMonthText,
        smTo.SalaryMonthText AS DeductionToSalaryMonthText,
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
    FROM ScheduledDeductions ssd
    INNER JOIN Employees e ON ssd.IdEmployee = e.IdEmployee
    LEFT JOIN Designations des ON e.IdDesignation = des.IdDesignation
    LEFT JOIN Departments dept ON e.IdDepartment = dept.IdDepartment
    LEFT JOIN SalaryMonths smFrom ON ssd.DeductionFromSalaryMonth = smFrom.IdSalaryMonth
    LEFT JOIN SalaryMonths smTo ON ssd.DeductionToSalaryMonth = smTo.IdSalaryMonth
    WHERE 1 = 1
    ");

        var parameters = new DynamicParameters();

        // Apply search filter if provided
        if (!string.IsNullOrEmpty(searchText))
        {
            query.Append(@"
        AND (
            e.EmployeeCode LIKE @SearchText OR
            CONCAT(e.FirstName, ' ', COALESCE(e.MiddleName, ''), ' ', e.LastName) LIKE @SearchText OR
            des.DesignationName LIKE @SearchText OR
            dept.DepartmentName LIKE @SearchText
        )
        ");
            parameters.Add("SearchText", $"%{searchText}%");
        }

        // Apply filter for DeductionFromSalaryMonthDate
        if (fromDate.HasValue)
        {
            query.Append(" AND ssd.DeductionFromSalaryMonthDate >= @FromDate");
            parameters.Add("FromDate", fromDate.Value);
        }

        // Order results
        query.Append(" ORDER BY e.FirstName, e.LastName;");

        try
        {
            using (var connection = _dbContext.Database.GetDbConnection())
            {
                if (connection.State == System.Data.ConnectionState.Closed)
                    await connection.OpenAsync();

                var deductions = await connection.QueryAsync<ScheduledSalaryDeductionDto>(query.ToString(), parameters);
                return deductions;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching scheduled salary deductions.");
            throw new Exception("An error occurred while fetching scheduled salary deductions. Please try again later.");
        }
    }


    public async Task<ScheduledSalaryDeductionDto?> GetScheduledDeductionById(int id)
    {
        var query = @"
    SELECT 
        ssd.IdScheduledSalaryDeduction,
        ssd.IdEmployee,
        ssd.TotalAmount,
        ssd.DeductionFromSalaryMonth,
        ssd.DeductionToSalaryMonth,
        ssd.DeductionFromSalaryMonthDate,
        ssd.DeductionToSalaryMonthDate,
        ssd.AllocatingSalaryHead,
        ssd.MonthCount,
        ssd.MonthlyDeductableAmount,
        smFrom.SalaryMonthText AS DeductionFromSalaryMonthText,
        smTo.SalaryMonthText AS DeductionToSalaryMonthText,
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
    FROM ScheduledDeductions ssd
    INNER JOIN Employees e ON ssd.IdEmployee = e.IdEmployee
    LEFT JOIN Designations des ON e.IdDesignation = des.IdDesignation
    LEFT JOIN Departments dept ON e.IdDepartment = dept.IdDepartment
    LEFT JOIN SalaryMonths smFrom ON ssd.DeductionFromSalaryMonth = smFrom.IdSalaryMonth
    LEFT JOIN SalaryMonths smTo ON ssd.DeductionToSalaryMonth = smTo.IdSalaryMonth
    WHERE ssd.IdScheduledSalaryDeduction = @Id
    ";

        try
        {
            using (var connection = _dbContext.Database.GetDbConnection())
            {
                if (connection.State == System.Data.ConnectionState.Closed)
                    await connection.OpenAsync();

                var deduction = await connection.QueryFirstOrDefaultAsync<ScheduledSalaryDeductionDto>(query, new { Id = id });
                return deduction;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching scheduled salary deduction with ID: {Id}", id);
            throw new Exception($"An error occurred while fetching the scheduled salary deduction with ID: {id}.", ex);
        }
    }


    public async Task<ScheduledSalaryDeductionDto?> AddScheduledDeduction(ScheduledSalaryDeductionDto dto, int EmployeeId)
    {
        try
        {
            var entity = _mapper.Map<ScheduledSalaryDeduction>(dto);

            var addedEntity = await _dbContext.ScheduledDeductions.AddAsync(entity);
            await _dbContext.SaveChangesAsync();

            return _mapper.Map<ScheduledSalaryDeductionDto>(addedEntity.Entity);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding scheduled salary deduction.");
            return null;
        }
    }

    public async Task<ScheduledSalaryDeductionDto?> UpdateScheduledDeduction(ScheduledSalaryDeductionDto dto, int EmployeeId)
    {
        try
        {
            var entity = await _dbContext.ScheduledDeductions.FindAsync(dto.IdScheduledSalaryDeduction);
            if (entity == null) return null;

            _mapper.Map(dto, entity);
            _dbContext.ScheduledDeductions.Update(entity);
            await _dbContext.SaveChangesAsync();

            return _mapper.Map<ScheduledSalaryDeductionDto>(entity);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating scheduled salary deduction.");
            return null;
        }
    }
}
