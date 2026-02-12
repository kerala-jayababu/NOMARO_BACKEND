using AutoMapper;
using Dapper;
using Georgetown_International_Academy.API.Database;
using Georgetown_Internationsl_Academy.API.DTO;
using Georgetown_Internationsl_Academy.API.Models;
using Georgetown_Internationsl_Academy.API.Services.Implimentation;
using Georgetown_Internationsl_Academy.API.Services.Interface;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using System.Text;

public class MaternityLeaveSalaryService : IMaternityLeaveSalaryService
{
    private readonly ApplicationDBContext _dbContext;
    private readonly IMapper _mapper;
    private readonly ILogger<MaternityLeaveSalaryService> _logger;
    private readonly IWebHostEnvironment _webHostEnvironment;
    private readonly IConfiguration _configuration;
    private readonly IApprovalWorkflowService _approvalWorkflowService;
    public MaternityLeaveSalaryService(ApplicationDBContext dbContext,  IMapper mapper, ILogger<MaternityLeaveSalaryService> logger, IWebHostEnvironment webHostEnvironment, IConfiguration configuration, IApprovalWorkflowService approvalWorkflowService)
    {
        _dbContext = dbContext;
        _mapper = mapper;
        _logger = logger;
        _webHostEnvironment = webHostEnvironment;
        _configuration = configuration;
        _approvalWorkflowService = approvalWorkflowService;
    }

    public async Task<IEnumerable<MaternityLeaveSalaryDto>> GetAllMaternityLeaveSalaries(string? searchText = null, DateTime? fromDate = null)
    {
        var query = new StringBuilder(@"
    SELECT 
        DISTINCT
        mls.IdMaternityLeaveSalary,
        mls.IdEmployee,
        e.FirstName, -- Added to resolve ORDER BY error
        e.LastName,  -- Added to resolve ORDER BY error
        CONCAT(e.FirstName, ' ', COALESCE(e.MiddleName, ''), ' ', e.LastName) AS EmployeeName,
        e.EmployeeCode,
        e.IdDesignation,
        des.DesignationName,
        e.IdDepartment,
        dept.DepartmentName,
        e.JoiningDate,
        e.Gender,
        e.EmailID,
        e.PhoneNumber1,
        e.PhoneNumber2,
        mls.IdSalaryMonthFrom,
        smFrom.SalaryMonthText AS FromSalaryMonthText,
        mls.IdSalaryMonthTo,
        smTo.SalaryMonthText AS ToSalaryMonthText,
        mls.MaternityLeaveFrom,
        mls.MaternityLeaveTo,
        mls.TotalEarnings,
        mls.DocumentFilePath,
        mls.TotalDeductions,
        mls.MaternityLeaveNetSalary,
        esc.NetSalary AS DefaultNetSalary
    FROM MaternityLeaveSalaries mls
    INNER JOIN Employees e ON mls.IdEmployee = e.IdEmployee
    INNER JOIN Designations des ON e.IdDesignation = des.IdDesignation
    INNER JOIN Departments dept ON e.IdDepartment = dept.IdDepartment
    LEFT JOIN SalaryMonths smFrom ON mls.IdSalaryMonthFrom = smFrom.IdSalaryMonth
    LEFT JOIN SalaryMonths smTo ON mls.IdSalaryMonthTo = smTo.IdSalaryMonth
    LEFT JOIN vw_LatestEmployeeSalaryConfig esc ON mls.IdEmployee = esc.IdEmployee
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

        // Apply filter for MaternityLeaveFrom date
        if (fromDate.HasValue)
        {
            query.Append(" AND mls.MaternityLeaveFrom >= @FromDate");
            parameters.Add("FromDate", fromDate.Value);
        }

        query.Append(@"
    ORDER BY 
        DefaultNetSalary DESC,
        e.FirstName,
        e.LastName
");

        try
        {
            using (var connection = _dbContext.Database.GetDbConnection())
            {
                if (connection.State == System.Data.ConnectionState.Closed)
                    await connection.OpenAsync();

                // Dapper will automatically map e.FirstName and e.LastName 
                // to your DTO if the properties exist; otherwise, it ignores them.
                var maternityLeaveSalaries = await connection.QueryAsync<MaternityLeaveSalaryDto>(query.ToString(), parameters);
                return maternityLeaveSalaries;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching Maternity Leave Salaries.");
            throw new Exception("An error occurred while fetching maternity leave salaries. Please try again later.");
        }
    }

    public async Task<MaternityLeaveSalaryDto?> GetMaternityLeaveSalaryById(int idMaternityLeaveSalary)
    {
        try
        {
            // Fetch Maternity Leave Salary
            var maternityLeaveSalary = await (from mls in _dbContext.MaternityLeaveSalaries
                                              join smFrom in _dbContext.SalaryMonths on mls.IdSalaryMonthFrom equals smFrom.IdSalaryMonth
                                              join smTo in _dbContext.SalaryMonths on mls.IdSalaryMonthTo equals smTo.IdSalaryMonth
                                              join e in _dbContext.Employees on mls.IdEmployee equals e.IdEmployee
                                              where mls.IdMaternityLeaveSalary == idMaternityLeaveSalary
                                              select new MaternityLeaveSalaryDto
                                              {
                                                  IdMaternityLeaveSalary = mls.IdMaternityLeaveSalary,
                                                  IdEmployee = mls.IdEmployee,
                                                  EmployeeName = string.Concat(e.FirstName, " ", e.MiddleName ?? "", " ", e.LastName).Trim(),
                                                  EmployeeCode = e.EmployeeCode,
                                                  IdSalaryMonthFrom = (int)mls.IdSalaryMonthFrom,
                                                  FromSalaryMonthText = smFrom.SalaryMonthText,
                                                  IdSalaryMonthTo = (int)mls.IdSalaryMonthTo,
                                                  ToSalaryMonthText = smTo.SalaryMonthText,
                                                  MaternityLeaveFrom = mls.MaternityLeaveFrom,
                                                  DocumentFilePath = mls.DocumentFilePath,
                                                  TotalEarnings =mls.TotalEarnings,
                                                  TotalDeductions =mls.TotalDeductions,                                                  
                                                  MaternityLeaveTo = mls.MaternityLeaveTo,
                                                  MaternityLeaveNetSalary = mls.MaternityLeaveNetSalary, // Corrected column name
                                                  MaternityLeaveSalaryDetailDto = new List<MaternityLeaveSalaryDetailDto>()
                                              }).FirstOrDefaultAsync();

            if (maternityLeaveSalary == null)
            {
                _logger.LogWarning("Maternity Leave Salary with ID {Id} not found.", idMaternityLeaveSalary);
                return null;
            }

            // Fetch Maternity Leave Salary Details
            maternityLeaveSalary.MaternityLeaveSalaryDetailDto = await (from mld in _dbContext.MaternityLeaveSalaryDetail
                                                                        where mld.IdMaternityLeaveSalary == idMaternityLeaveSalary
                                                                        select new MaternityLeaveSalaryDetailDto
                                                                        {
                                                                            IdMaternityLeaveSalary= mld.IdMaternityLeaveSalary,
                                                                            IdMaternityLeaveSalaryDetail=mld.IdMaternityLeaveSalaryDetail,
                                                                            IdSalaryHead = mld.IdSalaryHead,
                                                                            SalaryHeadName = mld.SalaryHeadName,
                                                                            SalaryHeadType = mld.SalaryHeadType,
                                                                            Amount = mld.Amount,
                                                                            AmountInUSD = mld.AmountInUSD,
                                                                            CreatedBy = mld.CreatedBy,
                                                                            CreatedDate = mld.CreatedDate
                                                                        }).ToListAsync();

            return maternityLeaveSalary;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching details for Maternity Leave Salary with ID: {Id}", idMaternityLeaveSalary);
            throw new Exception("An error occurred while fetching maternity leave salary details. Please try again later.");
        }
    }



    public async Task<MaternityLeaveSalaryDto?> AddMaternityLeaveSalary(MaternityLeaveSalaryDto dto,int EmployeeId)
    {

        using var transaction = await _dbContext.Database.BeginTransactionAsync();

        try
        {
            // Calculate Total Earnings and Deductions
            var totalEarnings = dto.MaternityLeaveSalaryDetailDto
                .Where(detail => detail.SalaryHeadType == "EARNING")
                .Sum(detail => detail.Amount ?? 0);

            var totalDeductions = dto.MaternityLeaveSalaryDetailDto
                .Where(detail => detail.SalaryHeadType == "DEDUCTION")
                .Sum(detail => detail.Amount ?? 0);

            // Insert into MaternityLeaveSalaries
            var maternityLeaveSalary = new MaternityLeaveSalaryEntity
            {
                IdEmployee = dto.IdEmployee,
                IdSalaryMonthFrom = dto.IdSalaryMonthFrom,
                IdSalaryMonthTo = dto.IdSalaryMonthTo,
                MaternityLeaveFrom = dto.MaternityLeaveFrom,
                MaternityLeaveTo = dto.MaternityLeaveTo,
                TotalEarnings = totalEarnings,
                TotalDeductions = totalDeductions,
                MaternityLeaveNetSalary = totalEarnings - totalDeductions,
                
            };

            await _dbContext.MaternityLeaveSalaries.AddAsync(maternityLeaveSalary);
            await _dbContext.SaveChangesAsync();

            if (dto.File != null)
            {
                string uploadFolderPath = Path.Combine(_webHostEnvironment.ContentRootPath, "Uploads/Documents");
                if (!Directory.Exists(uploadFolderPath))
                {
                    Directory.CreateDirectory(uploadFolderPath);
                }

                string fileExtension = Path.GetExtension(dto.File.FileName);
                string currentDate = DateTime.Now.ToString("yyyy_MM_dd");
                string originalFileNameWithoutExt = Path.GetFileNameWithoutExtension(dto.File.FileName);
                string uniqueFileName = $"ML_{maternityLeaveSalary.IdMaternityLeaveSalary}_{dto.IdEmployee}_{dto.EmployeeCode}_{currentDate}{fileExtension}";
                string filePath = Path.Combine(uploadFolderPath, uniqueFileName);
              
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await dto.File.CopyToAsync(stream);
                }

                // Update the document file path in the database
                maternityLeaveSalary.DocumentFilePath = filePath;

                _dbContext.MaternityLeaveSalaries.Update(maternityLeaveSalary);
                await _dbContext.SaveChangesAsync();
            }


            // Insert into MaternityLeaveSalaryDetails
            foreach (var detail in dto.MaternityLeaveSalaryDetailDto)
            {
                var maternityLeaveDetail = new MaternityLeaveSalaryDetail
                {
                    IdMaternityLeaveSalary = (int)maternityLeaveSalary.IdMaternityLeaveSalary,
                    IdSalaryHead = (int)detail.IdSalaryHead,
                    SalaryHeadName = detail.SalaryHeadName,
                    SalaryHeadType = detail.SalaryHeadType,
                    Amount = (decimal)detail.Amount,
                    AmountInUSD = 0,
                    CreatedBy = EmployeeId,
                    CreatedDate = DateTime.Now
                };

                await _dbContext.MaternityLeaveSalaryDetail.AddAsync(maternityLeaveDetail);
            }

            await _dbContext.SaveChangesAsync();
            var entityCode = _configuration["WorkflowEntityCodes:MaternityLeaveSalary"];

            //var approvalResult = await _approvalWorkflowService
            //    .InitiateApprovalWorkflow(
            //        (int)maternityLeaveSalary.IdMaternityLeaveSalary,
            //        entityCode,
            //        EmployeeId,
            //        "SUBMITTED",
            //        null,
            //        null);

            //if (approvalResult != "Approval workflow initiated.")
            //{
            //    throw new Exception(approvalResult);
            //}
            await transaction.CommitAsync();

            return _mapper.Map< MaternityLeaveSalaryDto >(maternityLeaveSalary);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Error creating Maternity Leave Salary.");
            throw new Exception("An error occurred while creating the Maternity Leave Salary. Please try again.");
        }
    }

    public async Task<MaternityLeaveSalaryDto?> UpdateMaternityLeaveSalary(MaternityLeaveSalaryDto dto, int updatedBy)
    {
        using var transaction = await _dbContext.Database.BeginTransactionAsync();

        try
        {
            // Fetch existing MaternityLeaveSalary
            var existingSalary = await _dbContext.MaternityLeaveSalaries
                .FirstOrDefaultAsync(mls => mls.IdMaternityLeaveSalary == dto.IdMaternityLeaveSalary);

            if (existingSalary == null)
            {
                _logger.LogWarning("Maternity Leave Salary with ID {Id} not found.", dto.IdMaternityLeaveSalary);
                return null;
            }

            // Update MaternityLeaveSalary fields
            existingSalary.MaternityLeaveFrom = dto.MaternityLeaveFrom;
            existingSalary.MaternityLeaveTo = dto.MaternityLeaveTo;
            existingSalary.IdSalaryMonthFrom = dto.IdSalaryMonthFrom;
            existingSalary.IdSalaryMonthTo = dto.IdSalaryMonthTo;
          
            // Calculate new Total Earnings and Deductions
            var totalEarnings = dto.MaternityLeaveSalaryDetailDto?
                .Where(d => d.SalaryHeadType == "EARNING")
                .Sum(d => d.Amount) ?? 0;

            var totalDeductions = dto.MaternityLeaveSalaryDetailDto?
                .Where(d => d.SalaryHeadType == "DEDUCTION")
                .Sum(d => d.Amount) ?? 0;

            existingSalary.TotalEarnings = totalEarnings;
            existingSalary.TotalDeductions = totalDeductions;
            existingSalary.MaternityLeaveNetSalary = totalEarnings - totalDeductions;

            
            _dbContext.MaternityLeaveSalaries.Update(existingSalary);
            await _dbContext.SaveChangesAsync();

            if (dto.File != null)
            {
                string uploadFolderPath = Path.Combine(_webHostEnvironment.ContentRootPath, "Uploads/Documents");
                if (!Directory.Exists(uploadFolderPath))
                {
                    Directory.CreateDirectory(uploadFolderPath);
                }

                string fileExtension = Path.GetExtension(dto.File.FileName);
                string currentDate = DateTime.Now.ToString("yyyy_MM_dd");
                string originalFileNameWithoutExt = Path.GetFileNameWithoutExtension(dto.File.FileName);
                string uniqueFileName = $"ML_{dto.IdMaternityLeaveSalary}_{dto.IdEmployee}_{dto.EmployeeCode}_{currentDate}{fileExtension}";
                string filePath = Path.Combine(uploadFolderPath, uniqueFileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await dto.File.CopyToAsync(stream);
                }

                // Update the document file path in the database
                existingSalary.DocumentFilePath = filePath;

                _dbContext.MaternityLeaveSalaries.Update(existingSalary);
                await _dbContext.SaveChangesAsync();
            }

            // Fetch existing details
            var existingDetails = await _dbContext.MaternityLeaveSalaryDetail
                .Where(mld => mld.IdMaternityLeaveSalary == dto.IdMaternityLeaveSalary)
                .ToListAsync();

            // Handle details: delete, update, add
            if (dto.MaternityLeaveSalaryDetailDto != null)
            {
                // Delete details not in the DTO
                var detailsToDelete = existingDetails
                    .Where(existing => !dto.MaternityLeaveSalaryDetailDto
                        .Any(d => d.IdMaternityLeaveSalaryDetail == existing.IdMaternityLeaveSalaryDetail))
                    .ToList();

                if (detailsToDelete.Any())
                {
                    _dbContext.MaternityLeaveSalaryDetail.RemoveRange(detailsToDelete);
                }

                // Update existing details and add new ones
                foreach (var detailDto in dto.MaternityLeaveSalaryDetailDto)
                {
                    var existingDetail = existingDetails
                        .FirstOrDefault(ed => ed.IdMaternityLeaveSalaryDetail == detailDto.IdMaternityLeaveSalaryDetail);

                    if (existingDetail != null)
                    {
                        // Update existing detail
                        existingDetail.IdSalaryHead = (int)detailDto.IdSalaryHead;
                        existingDetail.SalaryHeadName = detailDto.SalaryHeadName;
                        existingDetail.SalaryHeadType = detailDto.SalaryHeadType;
                        existingDetail.Amount = (decimal)detailDto.Amount;                        

                        _dbContext.MaternityLeaveSalaryDetail.Update(existingDetail);
                    }
                    else
                    {
                        // Add new detail
                        var newDetail = new MaternityLeaveSalaryDetail
                        {
                            IdMaternityLeaveSalary = (int)dto.IdMaternityLeaveSalary,
                            IdSalaryHead = (int)detailDto.IdSalaryHead,
                            SalaryHeadName = detailDto.SalaryHeadName,
                            SalaryHeadType = detailDto.SalaryHeadType,
                            Amount = (decimal)detailDto.Amount,
                            AmountInUSD = 0,
                            CreatedBy = updatedBy,
                            CreatedDate = DateTime.Now
                        };

                        await _dbContext.MaternityLeaveSalaryDetail.AddAsync(newDetail);
                    }
                }
            }

            await _dbContext.SaveChangesAsync();
            var entityCode = _configuration["WorkflowEntityCodes:MaternityLeaveSalary"];

            //var approvalResult = await _approvalWorkflowService
            //    .InitiateApprovalWorkflow(
            //        (int)existingSalary.IdMaternityLeaveSalary,
            //        entityCode,
            //        updatedBy,
            //        "SUBMITTED",
            //        null,
            //        null);

            //if (approvalResult != "Approval workflow initiated.")
            //{
            //    throw new Exception(approvalResult);
            //}
            await transaction.CommitAsync();

            // Map and return updated DTO
            return new MaternityLeaveSalaryDto
            {
                IdMaternityLeaveSalary = existingSalary.IdMaternityLeaveSalary,
                IdEmployee = existingSalary.IdEmployee,
                MaternityLeaveFrom = existingSalary.MaternityLeaveFrom,
                MaternityLeaveTo = existingSalary.MaternityLeaveTo,
                IdSalaryMonthFrom = (int)existingSalary.IdSalaryMonthFrom,
                IdSalaryMonthTo = (int)existingSalary.IdSalaryMonthTo,
                TotalEarnings = existingSalary.TotalEarnings,
                TotalDeductions = existingSalary.TotalDeductions,
                MaternityLeaveNetSalary = existingSalary.MaternityLeaveNetSalary,
                MaternityLeaveSalaryDetailDto = dto.MaternityLeaveSalaryDetailDto
            };
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Error updating Maternity Leave Salary with ID: {Id}", dto.IdMaternityLeaveSalary);
            throw new Exception("An error occurred while updating the maternity leave salary. Please try again later.");
        }
    }



}
