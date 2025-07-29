using AutoMapper;
using Dapper;
using Georgetown_International_Academy.API.Database;
using Georgetown_Internationsl_Academy.API.DTO.Time___Attendance.Shift;
using Georgetown_Internationsl_Academy.API.Services.Interface.Time___Attendance.Shift;
using Microsoft.EntityFrameworkCore;
using System.Data;
using System.Text;

namespace Georgetown_Internationsl_Academy.API.Services.Implimentation.Time___Attendance.Shift
{
    public class LeaveService : ILeaveService
    {
        private readonly ApplicationDBContext _dbContext;
        private readonly IMapper _mapper;
        private readonly ILogger<LeaveService> _logger;


        public LeaveService(ApplicationDBContext dbContext, IMapper mapper, ILogger<LeaveService> logger)
        {
            _dbContext = dbContext;
            _mapper = mapper;
            _logger = logger;
        }


        public async Task<IEnumerable<EmployeeLeaveReportDto>> GetEmployeeLeaveReport(int idEmployee, DateTime? dateFrom = null, DateTime? dateTo = null)
        {
            var query = new StringBuilder(@"
        SELECT 
            el.IdEmployeeLeave,
            el.IdEmployee,
            CONCAT(e.FirstName, ' ', ISNULL(e.MiddleName, ''), ' ', e.LastName) AS EmployeeName,
             e.IdDesignation,
            dsg.DesignationName,
            e.IdDepartment,
            dept.DepartmentName,
            el.LeaveFromDate,
            el.LeaveToDate,
            el.NoDays,
            el.AppliedDate,
            el.ApprovalStatus,
            el.LeaveTypeName,
            el.PayableStatus,
            el.SalaryTransactionType,
            el.SalaryTransactionID,
            el.SalaryAmountAdjusted
        FROM EmployeeLeaves el
        INNER JOIN Employees e ON el.IdEmployee = e.IdEmployee
        LEFT JOIN Designations dsg ON e.IdDesignation = dsg.IdDesignation
        LEFT JOIN Departments dept ON e.IdDepartment = dept.IdDepartment
        WHERE el.IdEmployee = @IdEmployee
        ");

            var parameters = new DynamicParameters();
            parameters.Add("IdEmployee", idEmployee);

            if (dateFrom.HasValue)
            {
                query.Append(" AND el.LeaveFromDate >= @DateFrom");
                parameters.Add("DateFrom", dateFrom.Value);
            }

            if (dateTo.HasValue)
            {
                query.Append(" AND el.LeaveToDate <= @DateTo");
                parameters.Add("DateTo", dateTo.Value);
            }

            query.Append(" ORDER BY el.AppliedDate DESC");

            try
            {
                using (var connection = _dbContext.Database.GetDbConnection())
                {
                    if (connection.State == ConnectionState.Closed)
                        await connection.OpenAsync();

                    var result = await connection.QueryAsync<EmployeeLeaveReportDto>(query.ToString(), parameters);
                    return result;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching employee leave report.");
                throw new Exception("An error occurred while fetching leave data. Please try again later.");
            }
        }

        public async Task<IEnumerable<EmployeeUnauthorizedAbsenceDto>> GetUnauthorizedAbsences(int idEmployee, DateTime? dateFrom = null, DateTime? dateTo = null)
        {
            var query = new StringBuilder(@"
        SELECT 
            eua.IdEmployee,
            CONCAT(e.FirstName, ' ', ISNULL(e.MiddleName, ''), ' ', e.LastName) AS EmployeeName,
            e.IdDesignation,
            dsg.DesignationName,
            e.IdDepartment,
            dept.DepartmentName,
            eua.AbsentDate,
            eua.Reason,
            eua.FilePath,
            eua.LeaveAdjusted,              
            eua.IdEmployeeLeave,           
            eua.IdEmployeeLeaveDetails       
        FROM EmployeeUnAuthorizedAbsence eua
        INNER JOIN Employees e ON eua.IdEmployee = e.IdEmployee
        LEFT JOIN Designations dsg ON e.IdDesignation = dsg.IdDesignation
        LEFT JOIN Departments dept ON e.IdDepartment = dept.IdDepartment
        WHERE eua.IdEmployee = @IdEmployee
        AND eua.IdEmployeeLeave IS NULL
        ");

            var parameters = new DynamicParameters();
            parameters.Add("IdEmployee", idEmployee);

            // If DateFrom is provided, add it to the query
            if (dateFrom.HasValue)
            {
                query.Append(" AND eua.AbsentDate >= @DateFrom");
                parameters.Add("DateFrom", dateFrom.Value);
            }

            // If DateTo is provided, add it to the query
            if (dateTo.HasValue)
            {
                query.Append(" AND eua.AbsentDate <= @DateTo");
                parameters.Add("DateTo", dateTo.Value);
            }

            query.Append(" ORDER BY eua.AbsentDate DESC");

            try
            {
                using (var connection = _dbContext.Database.GetDbConnection())
                {
                    if (connection.State == ConnectionState.Closed)
                        await connection.OpenAsync();

                    var result = await connection.QueryAsync<EmployeeUnauthorizedAbsenceDto>(query.ToString(), parameters);
                    return result;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching unauthorized absences.");
                throw new Exception("An error occurred while fetching unauthorized absences. Please try again later.");
            }
        }


    }

}
