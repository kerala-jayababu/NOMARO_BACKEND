using AutoMapper;
using Dapper;
using Nomaro.API.Database;
using Nomaro.API.DTO;
using Nomaro.API.Models;
using Nomaro.API.Services.Interface;
using iText.Commons.Actions.Contexts;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Org.BouncyCastle.Asn1.Esf;
using System.Net.Mime;
using System.Text;
using static Nomaro.API.Services.Implimentation.LeavePassageService;

namespace Nomaro.API.Services.Implimentation
{
    public class LeavePassageService : ILeavePassageService
    {
        private readonly ApplicationDBContext _dbContext;
        private readonly IMapper _mapper;
        private readonly ILogger<OvertimeTransactionService> _logger;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly IEmployeeServices _employeeServices;
        private readonly IConfiguration _configuration;
        private readonly IApprovalWorkflowService _approvalWorkflowService;
        private readonly IAuditService _auditService;

        public LeavePassageService(ApplicationDBContext dbContext, IMapper mapper, ILogger<OvertimeTransactionService> logger, IConfiguration configuration,
                                           IWebHostEnvironment webHostEnvironment, IEmployeeServices employeeServices, IApprovalWorkflowService approvalWorkflowService, IAuditService auditService)
        {
            _dbContext = dbContext;
            _mapper = mapper;
            _logger = logger;
            _webHostEnvironment = webHostEnvironment;
            _employeeServices = employeeServices;
            _configuration = configuration;
            _approvalWorkflowService = approvalWorkflowService;
            _auditService = auditService;
        }

        public async  Task<IEnumerable<LeavePassageDto>> GetLeavePassagesList(string? searchText, string? dropdownFilter = null)
        {
            var query = new StringBuilder(@"
        SELECT 
                lp.IdLeavePassage,
                lp.IdEmployee,
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
                lp.IdFinancialYear,
	            fy.WorkDateFrom as FinancialYearFrom,
	            fy.WorkDateTo as FinancialYearTo,
              lp.IdSalaryMonth,
              smFrom.SalaryMonthText,
              lp.Remarks,
              lp.ApprovalStatus
            FROM LeavePassages lp
            INNER JOIN Employees e ON lp.IdEmployee = e.IdEmployee
            INNER JOIN Designations des ON e.IdDesignation = des.IdDesignation
            INNER JOIN Departments dept ON e.IdDepartment = dept.IdDepartment
            LEFT JOIN SalaryMonths smFrom ON lp.IdSalaryMonth = smFrom.IdSalaryMonth
            LEFT JOIN WorkYears fy ON lp.IdSalaryMonth = fy.IdWorkYear
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
            if (!string.IsNullOrEmpty(dropdownFilter))
            {
                if (dropdownFilter.Equals("SUBMITTED", StringComparison.OrdinalIgnoreCase))
                {
                    // Include SUBMITTED and Interim Approved statuses
                    query.Append(@"
            AND (
                lp.ApprovalStatus = 'SUBMITTED'
                OR lp.ApprovalStatus = 'INTERIM APPROVED'
            )");
                }
                else if (dropdownFilter.Equals("APPROVED", StringComparison.OrdinalIgnoreCase))
                {
                    // Include only the record with MAX(ValidFrom) for APPROVED status
                    query.Append(@"
            AND lp.ApprovalStatus = 'APPROVED'");
                }
            }



            query.Append(" ORDER BY e.FirstName, e.LastName;");

            try
            {
                using (var connection = _dbContext.Database.GetDbConnection())
                {
                    if (connection.State == System.Data.ConnectionState.Closed)
                        await connection.OpenAsync();

                    var leavePassageDtos = await connection.QueryAsync<LeavePassageDto>(query.ToString(), parameters);
                    return leavePassageDtos;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching  Leave passage.");
                throw new Exception("An error occurred while fetching leave passage. Please try again later.");
            }
        }


        public async Task<IEnumerable<LeavePassageForHRDto>> GetLeavePassageRequestsForHR(
         int idWorkYear,
         string requestStatus,
         string? searchText,
         string? approvalStatus = null)
        {
            var query = new StringBuilder(@"
                SELECT 
                    lp.IdLeavePassage,
                    e.IdEmployee,
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
                    lp.IdFinancialYear,
                    fy.WorkDateFrom AS FinancialYearFrom,
                    fy.WorkDateTo AS FinancialYearTo,
                    lp.IdSalaryMonth,
                    smFrom.SalaryMonthText,
                    lp.Remarks,
                    lp.ApprovalStatus
                FROM Employees e
                INNER JOIN Designations des ON e.IdDesignation = des.IdDesignation
                INNER JOIN Departments dept ON e.IdDepartment = dept.IdDepartment

                LEFT JOIN LeavePassages lp 
                    ON lp.IdEmployee = e.IdEmployee 
                    AND lp.IdFinancialYear = @IdWorkYear

                LEFT JOIN SalaryMonths smFrom ON lp.IdSalaryMonth = smFrom.IdSalaryMonth
                LEFT JOIN WorkYears fy ON lp.IdFinancialYear = fy.IdWorkYear

                WHERE 1 = 1
            ");

            var parameters = new DynamicParameters();
            parameters.Add("IdWorkYear", idWorkYear);

            // 🔍 Search
            if (!string.IsNullOrWhiteSpace(searchText))
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

            // ✅ Request Status
            if (!string.IsNullOrWhiteSpace(requestStatus))
            {
                if (requestStatus.Equals("REQUESTED", StringComparison.OrdinalIgnoreCase))
                {
                    query.Append(" AND lp.IdSalaryMonth IS NOT NULL ");
                }
                else if (requestStatus.Equals("NOT REQUESTED", StringComparison.OrdinalIgnoreCase))
                {
                    query.Append(" AND lp.IdSalaryMonth IS NULL ");
                }
            }

            // ✅ Approval Status
            if (!string.IsNullOrWhiteSpace(approvalStatus))
            {
                if (approvalStatus.ToUpper() != "ALL")
                {
                    query.Append(" AND lp.ApprovalStatus = @ApprovalStatus ");
                    parameters.Add("ApprovalStatus", approvalStatus);
                }
            }

            query.Append(" ORDER BY e.FirstName, e.LastName;");

            try
            {
                using (var connection = _dbContext.Database.GetDbConnection())
                {
                    if (connection.State == System.Data.ConnectionState.Closed)
                        await connection.OpenAsync();

                    var result = await connection.QueryAsync<LeavePassageForHRDto>(
                        query.ToString(), parameters);

                    return result;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching Leave passage requests for HR.");
                throw new Exception("An error occurred while fetching leave passage. Please try again later.");
            }
        }

        public async Task<IEnumerable<LeavePassageDto>> GetLeavePassagesListByemployeeId(int EmployeeId)
        {
            var query = new StringBuilder(@"
                    SELECT 
                lp.IdLeavePassage,
                lp.IdEmployee,
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
                lp.IdFinancialYear,
	            fy.WorkDateFrom as FinancialYearFrom,
	            fy.WorkDateTo as FinancialYearTo,
              lp.IdSalaryMonth,
            lp.LeavePassageAmount,
              smFrom.SalaryMonthText,
              lp.Remarks,
              lp.ApprovalStatus
            FROM LeavePassages lp
            INNER JOIN Employees e ON lp.IdEmployee = e.IdEmployee
            INNER JOIN Designations des ON e.IdDesignation = des.IdDesignation
            INNER JOIN Departments dept ON e.IdDepartment = dept.IdDepartment
            LEFT JOIN SalaryMonths smFrom ON lp.IdSalaryMonth = smFrom.IdSalaryMonth
            LEFT JOIN WorkYears fy ON lp.IdSalaryMonth = fy.IdWorkYear
            WHERE lp.IdEmployee = @IdEmployee
                    ORDER BY e.FirstName, e.LastName;
                ");

            var parameters = new DynamicParameters();
            parameters.Add("@IdEmployee", EmployeeId);


            try
            {
                using (var connection = _dbContext.Database.GetDbConnection())
                {
                    if (connection.State == System.Data.ConnectionState.Closed)
                        await connection.OpenAsync();

                    var leavePassageDtos = await connection.QueryAsync<LeavePassageDto>(query.ToString(), parameters);
                    return leavePassageDtos;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching  Leave passage.");
                throw new Exception("An error occurred while fetching leave passage. Please try again later.");
            }
        }
        public async Task<LeavePassageDto?> GetLeavePassagesById(int id)
        {
            var query = new StringBuilder(@"
                SELECT 
                    lp.IdLeavePassage,
                    lp.IdEmployee,
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
                    lp.IdFinancialYear,
                    lp.LeavePassageAmount,
                    fy.WorkDateFrom as FinancialYearFrom,
                    fy.WorkDateTo as FinancialYearTo,
                    lp.IdSalaryMonth,
                    smFrom.SalaryMonthText,
                    lp.Remarks,
                    lp.ApprovalStatus,
                lpa.LeavePassageAmount as LeavePassageAmountFromLeavePassageAmount
                FROM LeavePassages lp
                INNER JOIN Employees e ON lp.IdEmployee = e.IdEmployee
                INNER JOIN Designations des ON e.IdDesignation = des.IdDesignation
                INNER JOIN Departments dept ON e.IdDepartment = dept.IdDepartment
                LEFT JOIN SalaryMonths smFrom ON lp.IdSalaryMonth = smFrom.IdSalaryMonth
                LEFT JOIN WorkYears fy ON lp.IdSalaryMonth = fy.IdWorkYear
             LEFT JOIN LeavePassageAmounts lpa 
                 ON lp.IdEmployee = lpa.IdEmployee 
                 AND lp.IdFinancialYear = lpa.IdFinancialYear
                WHERE lp.IdLeavePassage = @IdLeavePassage
                ");

            try
            {
                using (var connection = _dbContext.Database.GetDbConnection())
                {
                    if (connection.State == System.Data.ConnectionState.Closed)
                        await connection.OpenAsync();

                    var parameters = new DynamicParameters();
                    parameters.Add("IdLeavePassage", id);

                    var leavePassageDto = await connection.QueryFirstOrDefaultAsync<LeavePassageDto>(query.ToString(), parameters);
                    return leavePassageDto;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching Leave Passage by ID.");
                throw new Exception("An error occurred while fetching the leave passage. Please try again later.");
            }
        }

        public async Task<LeavePassageDto?> AddLeavePassages(LeavePassageDto leavePassage, int idEmployee)
        {
            try
            {
                var existingRec = await _dbContext.LeavePassages
                    .FirstOrDefaultAsync(l =>
                        l.IdFinancialYear == leavePassage.IdFinancialYear &&
                        l.IdEmployee == leavePassage.IdEmployee);

                if (existingRec != null)
                    throw new ArgumentException("Leave Passage already requested for the given year");

                var salaryMonthFrom = await _dbContext.SalaryMonths
                    .Where(sm => sm.IdSalaryMonth == leavePassage.IdSalaryMonth)
                    .Select(sm => sm.SalaryMonthFrom).FirstOrDefaultAsync();

                if (salaryMonthFrom == null)
                    throw new ArgumentException("Invalid salary month");
                var workyear = await _dbContext.WorkYears
                    .FirstOrDefaultAsync(wy =>
                        salaryMonthFrom.Date >= wy.WorkDateFrom.Value.Date &&
                        salaryMonthFrom.Date <= wy.WorkDateTo.Value.Date
                    );

                if (workyear == null)
                    throw new ArgumentException("No work year found for the selected salary month");

                var leavePassageEntity = _mapper.Map<LeavePassage>(leavePassage);

                leavePassageEntity.IdEmployee = idEmployee;
                leavePassageEntity.IdFinancialYear = workyear.IdWorkYear;
                leavePassageEntity.IdLeavePassage = null;
                leavePassageEntity.ApprovalStatus = "SUBMITTED";
                leavePassageEntity.CreatedDate = DateTime.Now;
                leavePassageEntity.CreatedBy = idEmployee;

                await _dbContext.LeavePassages.AddAsync(leavePassageEntity);
                await _dbContext.SaveChangesAsync();

                await _auditService.LogAuditAsync(
                    actionType: "Create",
                    entityName: "LeavePassage",
                    entityId: leavePassageEntity.IdLeavePassage ?? 0,
                    actionDetails: new { after = leavePassageEntity });

                int insertedId = leavePassageEntity.IdLeavePassage ?? 0;
                var entityCode = _configuration["WorkflowEntityCodes:LEAVEPASS"];

                await _approvalWorkflowService.InitiateApprovalWorkflow(
                    insertedId,
                    entityCode,
                    leavePassageEntity.IdEmployee,
                    "SUBMITTED",
                    null,
                    null);

                return _mapper.Map<LeavePassageDto>(leavePassageEntity);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding leave passage: {@Dto}.", leavePassage);
                throw new Exception(ex.Message);
            }
        }
        public async Task<LeavePassageDto?> UpdateLeavePassage(LeavePassageDto leavePassageDto, int IdEmployee)
        {
            try
            {
                var leavePassage = await _dbContext.LeavePassages
                    .FirstOrDefaultAsync(x => x.IdLeavePassage == leavePassageDto.IdLeavePassage);

                if (leavePassage == null) return null;

                var beforeUpdate = _mapper.Map<LeavePassage>(leavePassage);


                var salaryMonthFrom = await _dbContext.SalaryMonths
                    .Where(sm => sm.IdSalaryMonth == leavePassage.IdSalaryMonth)
                    .Select(sm => sm.SalaryMonthFrom)
                    .FirstOrDefaultAsync();

                if (salaryMonthFrom == null)
                    throw new ArgumentException("Invalid salary month");

                var workyear = await _dbContext.WorkYears
                     .FirstOrDefaultAsync(wy =>
                         salaryMonthFrom.Date >= wy.WorkDateFrom.Value.Date &&
                         salaryMonthFrom.Date <= wy.WorkDateTo.Value.Date
                     );

                if (workyear == null)
                    throw new ArgumentException("No work year found for the selected salary month");

                // Update fields
                leavePassage.IdEmployee = leavePassageDto.IdEmployee;
                leavePassage.IdFinancialYear = workyear.IdWorkYear;
                leavePassage.IdSalaryMonth = leavePassageDto.IdSalaryMonth;
                leavePassage.Remarks = leavePassageDto.Remarks;
                leavePassage.ApprovalStatus = leavePassageDto.ApprovalStatus;
                leavePassage.UpdatedOn = DateTime.Now;
                leavePassage.UpdatedBy = IdEmployee;
                _dbContext.LeavePassages.Update(leavePassage);
                await _dbContext.SaveChangesAsync();

                await _auditService.LogAuditAsync(
                    actionType: "Update",
                    entityName: "LeavePassage",
                    entityId: leavePassage.IdLeavePassage ?? 0,
                    actionDetails: new { before = beforeUpdate, after = leavePassage });

                var entityCode = _configuration["WorkflowEntityCodes:LEAVEPASS"];

                // Call approval workflow service
                var approvalResult = await _approvalWorkflowService.InitiateApprovalWorkflow(
                    leavePassage.IdLeavePassage ?? 0, entityCode, IdEmployee, "SUBMITTED", null,null);

                return _mapper.Map<LeavePassageDto>(leavePassage);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating leave passage with ID: {Id}.", leavePassageDto.IdLeavePassage);
                throw new Exception(ex.Message);
            }
        }
        public async Task<IEnumerable<LeavePassageAmountDto>> GetLeavePassageAmountDetails(
            int? financialYear = null,
            string? searchString = null)
        {
            var query = new StringBuilder(@"
                    SELECT
                        e.IdEmployee,
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

                        lpa.IdLeavePassageAmount,
                        lpa.IdFinancialYear,
                        lpa.DateFrom,
                        lpa.DateTo,
                        lpa.LeavePassageAmount,
                        lpa.PaidIdSalaryMonth,
                        lpa.Remarks,
                        fy.WorkDateFrom AS FinancialYearFrom,
                        fy.WorkDateTo   AS FinancialYearTo,

                        lp.IdLeavePassage,
                        lp.IdSalaryMonth,
                        CASE 
                            WHEN lpa.LeavePassageAmount < 0 THEN 'LP Reversal'
                            ELSE lp.ApprovalStatus
                        END AS LPRequestApprovalStatus,
                        smPaid.SalaryMonthText       AS PaidMonthName,
                        smPaid.SalaryMonthText AS PaidMonthText,      -- from lpa

                        smReq.SalaryMonthText        AS RequestedMonthName,
                        smReq.SalaryMonthText  AS RequestedMonthText  -- from lp

                    FROM Employees e
                    INNER JOIN Designations des ON e.IdDesignation = des.IdDesignation
                    INNER JOIN Departments  dept ON e.IdDepartment = dept.IdDepartment

                    LEFT JOIN WorkYears wy
                        ON (@FinancialYear IS NOT NULL AND wy.IdWorkYear = @FinancialYear)

                    LEFT JOIN LeavePassageAmounts lpa 
                        ON e.IdEmployee = lpa.IdEmployee
                        AND (@FinancialYear IS NULL OR lpa.IdFinancialYear = @FinancialYear)
                        AND (
                            @FinancialYear IS NULL
                            OR (
                                lpa.DateFrom >= wy.WorkDateFrom
                                AND lpa.DateTo   <= wy.WorkDateTo
                            )
                        )

                    LEFT JOIN LeavePassages lp
                        ON lp.IdEmployee       = lpa.IdEmployee
                        AND (@FinancialYear IS NULL OR lp.IdFinancialYear = @FinancialYear)

                    LEFT JOIN SalaryMonths smPaid
                        ON smPaid.IdSalaryMonth = lpa.PaidIdSalaryMonth

                    LEFT JOIN SalaryMonths smReq
                        ON smReq.IdSalaryMonth = lp.IdSalaryMonth

                    LEFT JOIN WorkYears fy 
                        ON lpa.IdFinancialYear = fy.IdWorkYear
                    WHERE e.IdEmployee >= 1000
                ");

            var parameters = new DynamicParameters();

            if (financialYear.HasValue && financialYear.Value > 0)
            {
                parameters.Add("FinancialYear", financialYear.Value);
            }
            else
            {
                // to avoid @FinancialYear being NULL but not defined
                parameters.Add("FinancialYear", null);
            }

            if (!string.IsNullOrEmpty(searchString))
            {
                query.Append(@"
            AND (
                e.EmployeeCode LIKE @SearchText OR
                CONCAT(e.FirstName, ' ', COALESCE(e.MiddleName, ''), ' ', e.LastName) LIKE @SearchText OR
                des.DesignationName LIKE @SearchText OR
                dept.DepartmentName LIKE @SearchText)");

                parameters.Add("SearchText", $"%{searchString}%");
            }

            query.Append(" ORDER BY e.FirstName, e.LastName;");

            try
            {
                using (var connection = _dbContext.Database.GetDbConnection())
                {
                    if (connection.State == System.Data.ConnectionState.Closed)
                        await connection.OpenAsync();

                    var result = await connection.QueryAsync<LeavePassageAmountDto>(
                        query.ToString(), parameters);

                    return result;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching Leave Passage Amount Details.");
                throw new Exception("An error occurred while fetching leave passage amount details.");
            }
        }

        public async Task<bool> SubmitLeavePassageAsync(List<LeavePassageAmountDetailsDto> leavePassages, int LoginedIdEmployee)
        {
            try
            {

                var auditLogs = new List<(string actionType, string entityName, int entityId, object actionDetails)>();

                foreach (var dto in leavePassages)
                {
                    var year = _dbContext.WorkYears.FirstOrDefault(x => x.IdWorkYear == dto.IdFinancialYear);
                    if (year == null) return false;

                    if (dto.IdLeavePassageAmount == 0) // Insert
                    {
                        var entity = new LeavePassageAmounts
                        {
                            IdEmployee = dto.IdEmployee,
                            LeavePassageAmount = dto.Amount,
                            IdFinancialYear = dto.IdFinancialYear,
                            DateFrom = year.WorkDateFrom,
                            DateTo = year.WorkDateTo,
                            PaidIdSalaryMonth = dto.PaidIdSalaryMonth,
                            CreatedBy = LoginedIdEmployee,
                            CreatedOn = DateTime.Today
                        };

                        _dbContext.LeavePassageAmounts.Add(entity);

                        // Collect audit log for insert (entityId will be set after SaveChangesAsync)
                        auditLogs.Add(("Add", "LeavePassageAmount", 0, new { after = entity }));
                    }
                    else // Update
                    {
                        var entity = await _dbContext.LeavePassageAmounts
                            .FirstOrDefaultAsync(x => x.IdLeavePassageAmount == dto.IdLeavePassageAmount);

                        if (entity == null) return false;

                        var beforeUpdate = new LeavePassageAmounts
                        {
                            IdLeavePassageAmount = entity.IdLeavePassageAmount,
                            IdEmployee = entity.IdEmployee,
                            LeavePassageAmount = entity.LeavePassageAmount,
                            IdFinancialYear = entity.IdFinancialYear,
                            DateFrom = entity.DateFrom,
                            DateTo = entity.DateTo
                        };

                        entity.IdEmployee = dto.IdEmployee;
                        entity.LeavePassageAmount = dto.Amount;
                        entity.IdFinancialYear = dto.IdFinancialYear;
                        entity.DateFrom = year.WorkDateFrom;
                        entity.DateTo = year.WorkDateTo;
                        entity.PaidIdSalaryMonth = dto.PaidIdSalaryMonth;
                        entity.ModifiedBy = LoginedIdEmployee;
                        entity.ModifiedOn = dto.ModifiedDate;
                        _dbContext.LeavePassageAmounts.Update(entity);

                        // Collect audit log for update
                        auditLogs.Add(("Update", "LeavePassageAmount", entity.IdLeavePassageAmount, new { before = beforeUpdate, after = entity }));
                    }
                }

                await _dbContext.SaveChangesAsync();

                // Now log audits with correct entityIds for inserts
                foreach (var (actionType, entityName, entityId, actionDetails) in auditLogs)
                {
                    int actualEntityId = entityId;
                    if (actionType == "Add" && actualEntityId == 0)
                    {
                        // For inserts, find the entityId from the actionDetails
                        var afterEntity = (LeavePassageAmounts)((dynamic)actionDetails).after;
                        actualEntityId = afterEntity.IdLeavePassageAmount;
                    }

                    await _auditService.LogAuditAsync(
                        actionType: actionType,
                        entityName: entityName,
                        entityId: actualEntityId,
                        actionDetails: actionDetails);
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while submitting Leave Passage Amounts");
                return false;
            }
        }

        public async Task<bool> SubmitLeavePassageReversalAsync(List<LeavePassageReversalDto> leavePassageReversal, int LoginedIdEmployee)
        {
            try
            {
                var financialYears = await _dbContext.FinancialYears.ToListAsync();

                if (financialYears == null || !financialYears.Any())
                    return false;

                foreach (var dto in leavePassageReversal)
                {
                    var year = _dbContext.WorkYears.FirstOrDefault(x => x.IdWorkYear == dto.IdFinancialYear);
                    if (year == null) return false;

                    if (dto.IdLeavePassageAmount == 0) // Insert
                    {
                        var entity = new LeavePassageAmounts
                        {
                            IdEmployee = dto.IdEmployee,
                            LeavePassageAmount = -Math.Abs(dto.ReversalAmount),
                            IdFinancialYear = dto.IdFinancialYear,
                            DateFrom = year.WorkDateFrom,
                            DateTo = year.WorkDateTo,
                            PaidIdSalaryMonth = dto.ReversalMonth,
                            Remarks = dto.Remarks,
                            CreatedBy = LoginedIdEmployee,
                            CreatedOn = DateTime.Today
                        };

                        _dbContext.LeavePassageAmounts.Add(entity);
                    }
                    else // Update
                    {
                        var entity = await _dbContext.LeavePassageAmounts
                            .FirstOrDefaultAsync(x => x.IdLeavePassageAmount == dto.IdLeavePassageAmount);

                        if (entity == null) return false;

                        entity.IdEmployee = dto.IdEmployee;
                        entity.LeavePassageAmount = -Math.Abs(dto.ReversalAmount);
                        entity.IdFinancialYear = dto.IdFinancialYear;
                        entity.DateFrom = year.WorkDateFrom;
                        entity.DateTo = year.WorkDateTo;
                        entity.PaidIdSalaryMonth = dto.ReversalMonth;
                        entity.Remarks = dto.Remarks;
                        entity.ModifiedBy = LoginedIdEmployee;
                        entity.ModifiedOn = DateTime.Today;
                        _dbContext.LeavePassageAmounts.Update(entity);
                    }
                }

                await _dbContext.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while submitting Leave Passage Reversal");
                return false;
            }
        }


        public async Task<bool> SubmitLeavePassageAdditionAsync(List<LeavePassageAdditionDto> leavePassageAddition, int LoginedIdEmployee)
        {
            try
            {
                var financialYears = await _dbContext.FinancialYears.ToListAsync();

                if (financialYears == null || !financialYears.Any())
                    return false;

                foreach (var dto in leavePassageAddition)
                {
                    var year = _dbContext.WorkYears.FirstOrDefault(x => x.IdWorkYear == dto.IdFinancialYear);
                    if (year == null) return false;

                    if (dto.IdLeavePassageAmount == 0) // Insert
                    {
                        var entity = new LeavePassageAmounts
                        {
                            IdEmployee = dto.IdEmployee,
                            LeavePassageAmount = dto.ReversalAmount,
                            IdFinancialYear = dto.IdFinancialYear,
                            DateFrom = year.WorkDateFrom,
                            DateTo = year.WorkDateTo,
                            PaidIdSalaryMonth = dto.ReversalMonth,
                            Remarks = dto.Remarks,
                            CreatedBy = LoginedIdEmployee,
                            CreatedOn = DateTime.Today
                        };

                        _dbContext.LeavePassageAmounts.Add(entity);
                    }
                    else // Update
                    {
                        var entity = await _dbContext.LeavePassageAmounts
                            .FirstOrDefaultAsync(x => x.IdLeavePassageAmount == dto.IdLeavePassageAmount);

                        if (entity == null) return false;

                        entity.IdEmployee = dto.IdEmployee;
                        entity.LeavePassageAmount = -Math.Abs(dto.ReversalAmount);
                        entity.IdFinancialYear = dto.IdFinancialYear;
                        entity.DateFrom = year.WorkDateFrom;
                        entity.DateTo = year.WorkDateTo;
                        entity.PaidIdSalaryMonth = dto.ReversalMonth;
                        entity.Remarks = dto.Remarks;
                        entity.ModifiedBy = LoginedIdEmployee;
                        entity.ModifiedOn = DateTime.Today;
                        _dbContext.LeavePassageAmounts.Update(entity);
                    }
                }

                await _dbContext.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while submitting Leave Passage Reversal");
                return false;
            }
        }

        public async Task<List<WorkMonthsInYearDto>> GetCurrentWorkYearMonths()
        {
            var today = DateTime.Today;

            // Step 1: Get current work year

            var workYear = await _dbContext.WorkYears.FirstOrDefaultAsync(w =>
                    today >= w.WorkDateFrom.Value && today <= w.WorkDateTo.Value);

            if (workYear == null || workYear.WorkDateFrom == null || workYear.WorkDateTo == null)
                return new List<WorkMonthsInYearDto>();

            var months = new List<WorkMonthsInYearDto>();

            // Step 2: Define start and end dates properly
            var start = new DateTime(workYear.WorkDateFrom.Value.Year, workYear.WorkDateFrom.Value.Month, 1);

            var end = new DateTime(workYear.WorkDateTo.Value.Year, workYear.WorkDateTo.Value.Month, 1);

            // Step 3: Get all salary months in one query (optimization)
            var salaryMonths = await _dbContext.SalaryMonths
                .Where(s => s.SalaryMonthDate >= start && s.SalaryMonthDate <= workYear.WorkDateTo)
                .ToListAsync();

            // Step 4: Loop month by month
            int Order = 1;
            while (start <= end)
            {
                var salaryMonth = salaryMonths
                    .FirstOrDefault(s => s.SalaryMonthDate.Year == start.Year &&
                                         s.SalaryMonthDate.Month == start.Month);

                months.Add(new WorkMonthsInYearDto
                {
                    IdOrder = Order,
                    MonthName = start.ToString("MMMM yyyy"),
                    IdSalaryMonth = salaryMonth?.IdSalaryMonth ?? 0,
                    IdWorkYEar = workYear.IdWorkYear
                });

                Order++;
                start = start.AddMonths(1);
            }

            return months;
        }

    }
}

