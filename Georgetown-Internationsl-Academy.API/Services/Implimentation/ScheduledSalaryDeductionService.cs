using AutoMapper;
using Dapper;
using Georgetown_International_Academy.API.Database;
using Georgetown_Internationsl_Academy.API.DTO;
using Georgetown_Internationsl_Academy.API.Models;
using Georgetown_Internationsl_Academy.API.Services.Implimentation;
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
        ssd.ScheduledSalaryDeductionDetails,
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
        var detailQuery = @"
SELECT 
    sdd.IdScheduledSalaryDeductionDetail,
    sdd.IdScheduledSalaryDeduction,
    sdd.IdSalaryMonth,
    sdd.AmountTobeDeducted,
    sm.SalaryMonthText AS SalaryMonthText
FROM ScheduledDeductionDetails sdd
LEFT JOIN SalaryMonths sm ON sdd.IdSalaryMonth = sm.IdSalaryMonth
WHERE sdd.IdScheduledSalaryDeduction IN (SELECT IdScheduledSalaryDeduction FROM ScheduledDeductions)
";

        try
        {
            using (var connection = _dbContext.Database.GetDbConnection())
            {
                if (connection.State == System.Data.ConnectionState.Closed)
                    await connection.OpenAsync();

                using (var multi = await connection.QueryMultipleAsync(query.ToString() + detailQuery, parameters))
                {
                    var deductions = (await multi.ReadAsync<ScheduledSalaryDeductionDto>()).ToList();
                    var deductionDetails = (await multi.ReadAsync<ScheduledDeductionDetailsDto>()).ToList();

                    // Map details to the main deduction list
                    var deductionDict = deductions.ToDictionary(d => d.IdScheduledSalaryDeduction);
                    foreach (var detail in deductionDetails)
                    {
                        if (deductionDict.TryGetValue(detail.IdScheduledSalaryDeduction, out var deduction))
                        {
                            deduction.ScheduledDeductionDetailsDto ??= new List<ScheduledDeductionDetailsDto>();
                            deduction.ScheduledDeductionDetailsDto.Add(detail);
                        }
                    }

                    return deductions;
                }
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
        ssd.ScheduledSalaryDeductionDetails,
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

SELECT 
    sdd.IdScheduledSalaryDeductionDetail,
    sdd.IdScheduledSalaryDeduction,
    sdd.IdSalaryMonth,
    sdd.AmountTobeDeducted,
    sm.SalaryMonthText AS SalaryMonthText
FROM ScheduledDeductionDetails sdd
LEFT JOIN SalaryMonths sm ON sdd.IdSalaryMonth = sm.IdSalaryMonth
WHERE sdd.IdScheduledSalaryDeduction = @Id;
    ";

        try
        {
            using (var connection = _dbContext.Database.GetDbConnection())
            {
                if (connection.State == System.Data.ConnectionState.Closed)
                    await connection.OpenAsync();

                using (var multi = await connection.QueryMultipleAsync(query, new { Id = id }))
                {
                    var deduction = await multi.ReadFirstOrDefaultAsync<ScheduledSalaryDeductionDto>();
                    if (deduction != null)
                    {
                        deduction.ScheduledDeductionDetailsDto = (await multi.ReadAsync<ScheduledDeductionDetailsDto>()).ToList();
                    }
                    return deduction;
                }
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
        using var transaction = await _dbContext.Database.BeginTransactionAsync();
        try
        {
            // Map and insert ScheduledSalaryDeduction
            var entity = _mapper.Map<ScheduledSalaryDeduction>(dto);
            entity.IdEmployee = EmployeeId;

            await _dbContext.ScheduledDeductions.AddAsync(entity);
            await _dbContext.SaveChangesAsync();

            // Insert related ScheduledDeductionDetails
            if (dto.ScheduledDeductionDetailsDto != null && dto.ScheduledDeductionDetailsDto.Any())
            {
                foreach (var detailDto in dto.ScheduledDeductionDetailsDto)
                {
                    var detailEntity = new ScheduledDeductionDetails
                    {
                        IdScheduledSalaryDeduction = entity.IdScheduledSalaryDeduction,
                        IdSalaryMonth = detailDto.IdSalaryMonth,
                        AmountTobeDeducted = detailDto.AmountTobeDeducted,
                        AmountDeducted = 0
                    };

                    _dbContext.ScheduledDeductionDetails.Add(detailEntity);
                }
                await _dbContext.SaveChangesAsync();
            }

            await transaction.CommitAsync();
            return _mapper.Map<ScheduledSalaryDeductionDto>(entity);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Error adding scheduled salary deduction.");
            return null;
        }
    }


    public async Task<ScheduledSalaryDeductionDto?> UpdateScheduledDeduction(ScheduledSalaryDeductionDto dto, int EmployeeId)
    {
        using var transaction = await _dbContext.Database.BeginTransactionAsync();

        try
        {
            // Step 1: Fetch Scheduled Deduction entity
            var entity = await _dbContext.ScheduledDeductions
                .FirstOrDefaultAsync(x => x.IdScheduledSalaryDeduction == dto.IdScheduledSalaryDeduction);

            if (entity == null)
            {
                _logger.LogWarning("Attempt to update a non-existent Scheduled Deduction with ID: {Id}.", dto.IdScheduledSalaryDeduction);
                return null;
            }

            // Step 2: Update Scheduled Deduction main entity
            entity.IdEmployee = dto.IdEmployee;
            entity.TotalAmount = dto.TotalAmount;
            entity.ScheduledSalaryDeductionDetails = dto.ScheduledSalaryDeductionDetails;
            entity.DeductionFromSalaryMonth = dto.DeductionFromSalaryMonth;
            entity.DeductionFromSalaryMonthDate = dto.DeductionFromSalaryMonthDate;
            entity.DeductionToSalaryMonth = dto.DeductionToSalaryMonth;
            entity.DeductionToSalaryMonthDate = dto.DeductionToSalaryMonthDate;
            entity.AllocatingSalaryHead = dto.AllocatingSalaryHead;
            entity.MonthCount = dto.MonthCount;
            entity.MonthlyDeductableAmount = dto.MonthlyDeductableAmount;
            
            _dbContext.ScheduledDeductions.Update(entity);
            await _dbContext.SaveChangesAsync();

            // Step 3: Update Scheduled Salary Deduction Details
            // Step 3: Update Scheduled Salary Deduction Details
            if (dto.ScheduledDeductionDetailsDto != null)
            {
                var existingDetails = await _dbContext.ScheduledDeductionDetails
                    .Where(d => d.IdScheduledSalaryDeduction == entity.IdScheduledSalaryDeduction)
                    .ToListAsync();

                // Delete details that are not in the DTO
                var detailsToDelete = existingDetails
                    .Where(d => !dto.ScheduledDeductionDetailsDto
                    .Any(dtoDetail => dtoDetail.IdScheduledSalaryDeductionDetail == d.IdScheduledSalaryDeductionDetail))
                    .ToList();
                _dbContext.ScheduledDeductionDetails.RemoveRange(detailsToDelete);

                foreach (var detailDto in dto.ScheduledDeductionDetailsDto)
                {
                    var existingDetail = existingDetails
                        .FirstOrDefault(d => d.IdScheduledSalaryDeductionDetail == detailDto.IdScheduledSalaryDeductionDetail);

                    if (existingDetail != null)
                    {
                        // Update existing detail
                        existingDetail.IdSalaryMonth = detailDto.IdSalaryMonth;
                        existingDetail.AmountTobeDeducted = detailDto.AmountTobeDeducted;
                        _dbContext.ScheduledDeductionDetails.Update(existingDetail);
                    }
                    else
                    {
                        var newDetail = new ScheduledDeductionDetails
                        {
                            IdScheduledSalaryDeduction = entity.IdScheduledSalaryDeduction,

                            IdSalaryMonth = detailDto.IdSalaryMonth,
                            AmountTobeDeducted = detailDto.AmountTobeDeducted,
                            AmountDeducted = 0                           
                        };

                        await _dbContext.ScheduledDeductionDetails.AddAsync(newDetail);

                    }
                }

                 
            }
            await _dbContext.SaveChangesAsync();
            await transaction.CommitAsync();
            return _mapper.Map<ScheduledSalaryDeductionDto>(entity);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Error updating Scheduled Deduction and Details.");
            throw new Exception("An error occurred while updating the scheduled deduction. Please try again later.");
        }
    }

}
