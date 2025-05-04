
using AutoMapper;
using Dapper;
using Georgetown_International_Academy.API.Database;
using Georgetown_Internationsl_Academy.API.DTO;
using Georgetown_Internationsl_Academy.API.Models;
using Georgetown_Internationsl_Academy.API.Services.Interface;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using System.Text;
namespace Georgetown_Internationsl_Academy.API.Services.Implimentation
{

    public class SalaryAdjustmentService : ISalaryAdjustmentService
    {
        private readonly ApplicationDBContext _dbContext;
        private readonly IMapper _mapper;
        private readonly ILogger<SalaryAdjustmentService> _logger;
        private readonly IWebHostEnvironment _webHostEnvironment;
        public SalaryAdjustmentService(ApplicationDBContext dbContext, IMapper mapper, ILogger<SalaryAdjustmentService> logger, IWebHostEnvironment webHostEnvironment)
        {
            _dbContext = dbContext;
            _mapper = mapper;
            _logger = logger;
            _webHostEnvironment = webHostEnvironment;
        }

        public async Task<IEnumerable<SalaryAdjustmentDto>> GetAllSalaryAdjustments(string? searchText = null, DateTime? fromDate = null)
        {
            var query = new StringBuilder(@"
    SELECT 
        sa.IdEmployee,
        e.EmployeeCode,
        CONCAT(e.FirstName, ' ', COALESCE(e.MiddleName, ''), ' ', e.LastName) AS EmployeeName,
        e.IdDesignation,
        des.DesignationName,
        e.IdDepartment,
        dept.DepartmentName,
        e.JoiningDate,
        e.Gender,
        sa.TANumber,
        sa.IsTaxable,
        e.EmailID,
        e.PhoneNumber1 AS PhoneNumber1,
 e.PhoneNumber2 AS PhoneNumber2,
e.CurrentStatus ,
        sa.IdSalaryAdjustment,
        sa.PayAdjustmentDate,
        sa.PayAdjustmentDetails,
        sa.DocumentFilePath,
        sa.AllocatingSalaryHead,
        sh.SalaryHeadName AS AllcoatingSalaryHeadName,
        sa.EarningOrDeduction,
        sa.AllocatingSalaryMonth,
        sm.SalaryMonthText AS AllocatingSalaryMonthText,
        sa.Amount,
        sa.Remarks
    FROM SalaryAdjustments sa
    LEFT JOIN Employees e ON sa.IdEmployee = e.IdEmployee
    LEFT JOIN Designations des ON e.IdDesignation = des.IdDesignation
    LEFT JOIN Departments dept ON e.IdDepartment = dept.IdDepartment
    LEFT JOIN SalaryHeads sh ON sa.AllocatingSalaryHead = sh.IdSalaryHead
    LEFT JOIN SalaryMonths sm ON sa.AllocatingSalaryMonth = sm.IdSalaryMonth
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

            // Apply filter for PayAdjustmentDate
            if (fromDate.HasValue)
            {
                query.Append(" AND sa.PayAdjustmentDate >= @FromDate");
                parameters.Add("FromDate", fromDate.Value);
            }

            // Order the results
            query.Append(" ORDER BY e.FirstName, e.LastName;");

            try
            {
                using (var connection = _dbContext.Database.GetDbConnection())
                {
                    if (connection.State == System.Data.ConnectionState.Closed)
                        await connection.OpenAsync();

                    var salaryAdjustments = await connection.QueryAsync<SalaryAdjustmentDto>(query.ToString(), parameters);
                    return salaryAdjustments;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching Salary Adjustments.");
                throw new Exception("An error occurred while fetching salary adjustments. Please try again later.");
            }
        }

        public async Task<SalaryAdjustmentDto?> GetSalaryAdjustmentById(int id)
        {
            var query = @"
    SELECT 
        sa.IdEmployee,
        e.EmployeeCode,
        CONCAT(e.FirstName, ' ', COALESCE(e.MiddleName, ''), ' ', e.LastName) AS EmployeeName,
        e.IdDesignation,
        des.DesignationName,
        e.IdDepartment,
        dept.DepartmentName,
        e.JoiningDate,
        e.Gender,
        e.EmailID,
        e.PhoneNumber1 AS PhoneNumber1,
        e.PhoneNumber2 AS PhoneNumber2,
        e.CurrentStatus,
        sa.IdSalaryAdjustment,
        sa.DocumentFilePath,
         sa.TANumber,
        sa.AllocationSalaryMonthDate,
        sa.PayAdjustmentDate,
        sa.PayAdjustmentDetails,
        sa.AllocatingSalaryHead,
        sh.SalaryHeadName AS AllcoatingSalaryHeadName,
        sa.EarningOrDeduction,
        sa.AllocatingSalaryMonth,
        sm.SalaryMonthText AS AllocatingSalaryMonthText,
        sa.Amount,
        sa.Remarks
    FROM SalaryAdjustments sa
    LEFT JOIN Employees e ON sa.IdEmployee = e.IdEmployee
    LEFT JOIN Designations des ON e.IdDesignation = des.IdDesignation
    LEFT JOIN Departments dept ON e.IdDepartment = dept.IdDepartment
    LEFT JOIN SalaryHeads sh ON sa.AllocatingSalaryHead = sh.IdSalaryHead
    LEFT JOIN SalaryMonths sm ON sa.AllocatingSalaryMonth = sm.IdSalaryMonth
    WHERE sa.IdSalaryAdjustment = @Id
    ";

            try
            {
                using (var connection = _dbContext.Database.GetDbConnection())
                {
                    if (connection.State == System.Data.ConnectionState.Closed)
                        await connection.OpenAsync();

                    var adjustment = await connection.QueryFirstOrDefaultAsync<SalaryAdjustmentDto>(query, new { Id = id });
                    return adjustment;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching salary adjustment with ID: {Id}", id);
                throw new Exception($"An error occurred while fetching the salary adjustment with ID: {id}.", ex);
            }
        }


        public async Task<SalaryAdjustmentDto?> AddSalaryAdjustment(SalaryAdjustmentDto salaryAdjustment)
        {
            using var transaction = await _dbContext.Database.BeginTransactionAsync();

            try
            {
                var entity = _mapper.Map<SalaryAdjustment>(salaryAdjustment);

                // Save the entity first to generate IdSalaryAdjustment
                var addedEntity = await _dbContext.SalaryAdjustments.AddAsync(entity);
                await _dbContext.SaveChangesAsync();

                // Handle file upload
                if (salaryAdjustment.File != null)
                {
                    string uploadFolder = Path.Combine(_webHostEnvironment.ContentRootPath, "Uploads/Documents");

                    if (!Directory.Exists(uploadFolder))
                        Directory.CreateDirectory(uploadFolder);

                    string fileExtension = Path.GetExtension(salaryAdjustment.File.FileName);
                    string timestamp = DateTime.Now.ToString("yyyy_MM_dd");
                    string originalFileNameWithoutExt = Path.GetFileNameWithoutExtension(salaryAdjustment.File.FileName);
                    string fileName = $"SA_{addedEntity.Entity.IdSalaryAdjustment}_{salaryAdjustment.IdEmployee}_{salaryAdjustment.EmployeeCode}_{timestamp}_{originalFileNameWithoutExt}{fileExtension}";
                    string filePath = Path.Combine(uploadFolder, fileName);

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await salaryAdjustment.File.CopyToAsync(stream);
                    }

                    // Update the file path in the entity
                    addedEntity.Entity.DocumentFilePath = filePath;
                    _dbContext.SalaryAdjustments.Update(addedEntity.Entity);
                    await _dbContext.SaveChangesAsync();
                }

                await transaction.CommitAsync();

                return _mapper.Map<SalaryAdjustmentDto>(addedEntity.Entity);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error adding salary adjustment.");
                return null;
            }
        }


        public async Task<SalaryAdjustmentDto?> UpdateSalaryAdjustment(SalaryAdjustmentDto salaryAdjustment)
        {
            using var transaction = await _dbContext.Database.BeginTransactionAsync();

            try
            {
                var existingAdjustment = await _dbContext.SalaryAdjustments
                    .FirstOrDefaultAsync(x => x.IdSalaryAdjustment == salaryAdjustment.IdSalaryAdjustment);

                if (existingAdjustment == null)
                    return null;

                // Save old file path before mapping
                var oldFilePath = existingAdjustment.DocumentFilePath;

                // Map updated fields
                _mapper.Map(salaryAdjustment, existingAdjustment);

                // Handle file upload (if new file provided)
                if (salaryAdjustment.File != null)
                {
                    string uploadFolder = Path.Combine(_webHostEnvironment.ContentRootPath, "Uploads/Documents");

                    if (!Directory.Exists(uploadFolder))
                        Directory.CreateDirectory(uploadFolder);

                    // Delete old file if exists
                    if (!string.IsNullOrWhiteSpace(oldFilePath) && File.Exists(oldFilePath))
                        File.Delete(oldFilePath);

                    string fileExtension = Path.GetExtension(salaryAdjustment.File.FileName);
                    string timestamp = DateTime.Now.ToString("yyyy_MM_dd");
                    string originalFileNameWithoutExt = Path.GetFileNameWithoutExtension(salaryAdjustment.File.FileName);
                    string fileName = $"SA_{existingAdjustment.IdSalaryAdjustment}_{salaryAdjustment.IdEmployee}_{salaryAdjustment.EmployeeCode}_{timestamp}_{originalFileNameWithoutExt}{fileExtension}";
                    string filePath = Path.Combine(uploadFolder, fileName);

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await salaryAdjustment.File.CopyToAsync(stream);
                    }

                    existingAdjustment.DocumentFilePath = filePath;
                }

                _dbContext.SalaryAdjustments.Update(existingAdjustment);
                await _dbContext.SaveChangesAsync();
                await transaction.CommitAsync();

                return _mapper.Map<SalaryAdjustmentDto>(existingAdjustment);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error updating salary adjustment.");
                return null;
            }
        }

    }

}
