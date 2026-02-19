using AutoMapper;
using Dapper;
using Georgetown_International_Academy.API.Database;
using Georgetown_Internationsl_Academy.API.DTO;
using Georgetown_Internationsl_Academy.API.DTO.Shift;
using Georgetown_Internationsl_Academy.API.DTO.Time___Attendance.Shift;
using Georgetown_Internationsl_Academy.API.Models.Shift;
using Georgetown_Internationsl_Academy.API.Models.Time___Attendance.Shift;
using Georgetown_Internationsl_Academy.API.Services.Interface;
using Georgetown_Internationsl_Academy.API.Services.Interface.Shift;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System.Data;
using System.Text;

public class ShiftService : IShiftService
{
    private readonly ApplicationDBContext _dbContext;
    private readonly IMapper _mapper;
    private readonly ILogger<ShiftService> _logger;
    private readonly IConfiguration _configuration;
    private readonly IApprovalWorkflowService _approvalWorkflowService;

    public ShiftService(ApplicationDBContext dbContext, IMapper mapper, ILogger<ShiftService> logger, IConfiguration configuration, IApprovalWorkflowService approvalWorkflowService)
    {
        _dbContext = dbContext;
        _mapper = mapper;
        _logger = logger;
        _configuration = configuration;
        _approvalWorkflowService = approvalWorkflowService;
    }

    public async Task<IEnumerable<ShiftDto>> GetShiftList()
    {
        try
        {
            var shifts = await _dbContext.ShiftDefinitions.OrderBy(s => s.ShiftName).ToListAsync();
            return _mapper.Map<IEnumerable<ShiftDto>>(shifts);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching shift list.");
            throw;
        }
    }

    public async Task<ShiftDto?> GetShiftById(int id)
    {
        try
        {
            var shift = await _dbContext.ShiftDefinitions.FirstOrDefaultAsync(s => s.IdShift == id);
            return shift == null ? null : _mapper.Map<ShiftDto>(shift);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching shift by ID {Id}", id);
            throw;
        }
    }

    public async Task<ShiftDto?> AddShift(ShiftDto shiftDto)
    {
        try
        {
            var entity = _mapper.Map<ShiftDefinitionEntity>(shiftDto);
            var result = await _dbContext.ShiftDefinitions.AddAsync(entity);
            await _dbContext.SaveChangesAsync();
            return _mapper.Map<ShiftDto>(result.Entity);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding shift");
            return null;
        }
    }

    public async Task<ShiftDto?> UpdateShift(ShiftDto shiftDto)
    {
        try
        {
            var existing = await _dbContext.ShiftDefinitions.FirstOrDefaultAsync(s => s.IdShift == shiftDto.IdShift);
            if (existing == null)
            {
                _logger.LogWarning("Shift with ID {Id} not found", shiftDto.IdShift);
                return null;
            }

            existing.ShiftName = shiftDto.ShiftName;
            _dbContext.ShiftDefinitions.Update(existing);
            await _dbContext.SaveChangesAsync();
            return _mapper.Map<ShiftDto>(existing);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating shift with ID {Id}", shiftDto.IdShift);
            return null;
        }
    }



    public async Task<List<ClockInOutDto>> GetClockInClockOutDetailsAsync(string? idEmployee,DateTime dateFrom,DateTime dateTo,int? idDepartment,bool? missingEntryOnly)
    {
        try
        {
            using (var connection = _dbContext.Database.GetDbConnection() as SqlConnection)
            {
                var parameters = new DynamicParameters();
                parameters.Add("@IdEmployeeString", idEmployee, DbType.String);
                parameters.Add("@Datefrom", dateFrom, DbType.DateTime);
                parameters.Add("@DateTo", dateTo, DbType.DateTime);
                parameters.Add("@IdDepartment", idDepartment, DbType.Int32);

                // Convert bool? to "Yes"/"No" or null
                string? missingEntryString = missingEntryOnly.HasValue ? (missingEntryOnly.Value ? "Yes" : "No") : null;
                parameters.Add("@MissingEntryOnly", missingEntryString, DbType.String);

                var result = await connection.QueryAsync<ClockInOutDto>(
                    "GetClockInClockOutDetails",
                    parameters,
                    commandType: CommandType.StoredProcedure);               

                return result.ToList();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving clock-in/out details for parameters");
            throw new Exception("An error occurred while retrieving clock-in/out details. Please try again later.");
        }
    }

    public async Task<List<ClockInOutDetailsDateGroupedDto>> GetClockInClockOutDetailsOfEmployeeGroupedByDate(
        int idEmployee,DateTime dateFrom,DateTime dateTo)
    {
        try
        {
            var result = new List<ClockInOutDetailsDateGroupedDto>();

            DateTime currentDate = dateTo.Date;
            int orderNumber = 1;

            while (currentDate >= dateFrom.Date)
            {
                var clockDetails = await _dbContext.ClockInOutDetails
                    .Where(c =>
                        c.IdEmployee == idEmployee &&
                        c.ClockDate.Date == currentDate)
                    .Select(c => new EmployeeClockDetails
                    {
                        IdClockDetails = c.IdClockDetails,
                        INTime = c.INTime,
                        OUTTime = c.OUTTime,
                        TotalINHours = c.TotalINHours,
                        TotalInminutes = c.TotalInMinutes,
                        TotalInHoursText = c.TotalInHoursText,
                        StatusDetails = c.StatusDetails
                    }).OrderBy(cc=>cc.INTime)
                    .ToListAsync();

                var grouped = new ClockInOutDetailsDateGroupedDto
                {
                    ClockDate = currentDate,
                    OrderNumber = orderNumber,
                    ClockDetails = clockDetails
                };

                result.Add(grouped);
                currentDate = currentDate.AddDays(-1);
                orderNumber++;
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving clock-in/out details.");
            throw new Exception("An error occurred while retrieving clock-in/out details. Please try again later.");
        }
    }


    public async Task<IEnumerable<DayAttendanceDto>> GetDayAttendanceDetails(
    DateTime dateFrom,
    DateTime dateTo,
    List<int> idEmployees,
    int? idDepartment = null)
    {
        var query = new StringBuilder(@"
            SELECT 
                e.IdEmployee,
                CONCAT(e.FirstName, ' ', ISNULL(e.MiddleName, ''), ' ', e.LastName) AS EmployeeName,
                e.IdDesignation,
                dsg.DesignationName,
                e.IdDepartment,
        e.EmployeeCode,
                dept.DepartmentName,
                da.AttendanceDate,
                da.RegularDayType,
                da.IdShiftSchedule,
                da.FirstInDateTime,
                da.LastOutDateTime,
                da.ExpectedInDateTime,
                da.ExpectedOutDateTime,
                da.TotalDurationInMinutes,
                da.TotalDurationInHours,
                da.ActualDurationInMinutes,
                da.ActualDurationInHours,
                da.ExpectedDurationInMinutes,
                da.MinuteDifference,
                da.AllowedTolerenceInMinutes,
                da.DeficitHours,
       	         CAST(FLOOR(da.TotalDurationInHours) AS VARCHAR) + ' Hrs ' + 
		        CAST(CAST((da.TotalDurationInHours - FLOOR(da.TotalDurationInHours)) * 60 AS INT) AS VARCHAR) + ' Min' AS TotalDurationHoursText,
		        CAST(FLOOR(da.ActualDurationInHours) AS VARCHAR) + ' Hrs ' + 
		        CAST(CAST((da.ActualDurationInHours - FLOOR(da.ActualDurationInHours)) * 60 AS INT) AS VARCHAR) + ' Min' AS ActualHoursText,
                da.StatusType,
                da.StatusDetails,
                da.ReasonForShortTime,
                da.TimeSheetApprovalStatus,
                da.IdDayAttendance
            FROM DayAttendance da
            INNER JOIN Employees e ON da.IdEmployee = e.IdEmployee
            INNER JOIN Departments dept ON e.IdDepartment = dept.IdDepartment
            INNER JOIN Designations dsg ON e.IdDesignation = dsg.IdDesignation
            WHERE da.AttendanceDate BETWEEN @DateFrom AND @DateTo
        ");

        var parameters = new DynamicParameters();
        parameters.Add("DateFrom", dateFrom);
        parameters.Add("DateTo", dateTo);

        if (idEmployees != null && idEmployees.Any())
        {
            query.Append(" AND da.IdEmployee IN @IdEmployees");
            parameters.Add("IdEmployees", idEmployees);
        }

        if (idDepartment.HasValue)
        {
            query.Append(" AND e.IdDepartment = @IdDepartment");
            parameters.Add("IdDepartment", idDepartment);
        }

        query.Append(" ORDER BY da.AttendanceDate DESC, EmployeeName");

        try
        {
            using (var connection = _dbContext.Database.GetDbConnection())
            {
                if (connection.State == ConnectionState.Closed)
                    await connection.OpenAsync();

                var result = await connection.QueryAsync<DayAttendanceDto>(query.ToString(), parameters);
                return result;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching day attendance details.");
            throw new Exception("An error occurred while fetching attendance data. Please try again later.");
        }
    }

    public async Task<bool> ApproveTimesheetAsync(List<ApproveTimesheetDto> dtos, int employeeId)
    {
        try
        {
            var ids = dtos.Select(d => d.IdDayAttendance).ToList();

            var records = await _dbContext.Set<DayAttendance>()
                .Where(a => ids.Contains(a.IdDayAttendance))
                .ToListAsync();

            if (!records.Any())
                return false;

            var currentTime = DateTime.Now;

            foreach (var record in records)
            {
                var dto = dtos.FirstOrDefault(d => d.IdDayAttendance == record.IdDayAttendance);
                if (dto == null) continue;

                record.TimeSheetApprovalStatus = dto.ApprovalStatus;
                record.ApprovedDateTime = currentTime;
                record.IdApprovedBy = employeeId;
                record.StatusDetails = dto.RejectReasons;
            }

            _dbContext.UpdateRange(records);
            await _dbContext.SaveChangesAsync();

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error approving timesheets in batch.");
            throw;
        }
    }


    public async Task<bool> UpdateClockInOutMissingEntriesAsync(List<UpdateClockInOutMissingEntryDto> dtos,int employeeid)
    {
        try
        {
            var ids = dtos.Select(d => d.IdClockDetail).ToList();

            var records = await _dbContext.Set<ClockInOutDetails>()
                .Where(c => ids.Contains(c.IdClockDetails))
                .ToListAsync();

            if (records.Count != dtos.Count)
                return false; // Some records not found

            foreach (var dto in dtos)
            {
                var record = records.FirstOrDefault(r => r.IdClockDetails == dto.IdClockDetail);
                if (record == null)
                    return false;

                if (dto.ClockType == "IN")
                {
                    record.INTime = dto.Time;
                    record.StatusDetails = "Missing-ManualInEntry";
                }
                    
                else if (dto.ClockType == "OUT")
                {
                    record.StatusDetails = "Missing-ManualOutEntry";
                    record.OUTTime = dto.Time;
                }                  

                record.Remarks = dto.Reason;
                if (record.INTime != null && record.OUTTime != null)
                {
                    var totalDuration = record.OUTTime.Value - record.INTime.Value;
                    record.TotalINHours = (decimal?)Math.Round(totalDuration.TotalHours, 2);
                    record.TotalInMinutes = (int)totalDuration.TotalMinutes;

                    int hours = totalDuration.Hours;
                    int minutes = totalDuration.Minutes;
                    record.TotalInHoursText = $"{hours} hrs {minutes} minutes";
                }
            }

            _dbContext.UpdateRange(records);
            var entityCode = _configuration["WorkflowEntityCodes:MissingEntry"];
            int count = 1;

            // Trigger the approval workflow for each updated record (clock entry)
            foreach (var record in records)
            {
                var result = await _approvalWorkflowService.InitiateApprovalWorkflow(
                    record.IdClockDetails, // Assuming IdClockDetails as the entity identifier
                    entityCode,
                    employeeid, // You need to pass the employee who initiated the update
                    "SUBMITTED", // Or use an appropriate status for this workflow
                    null, // Additional parameters if required by the workflow
                    null, // Any additional required data
                    count // A count to uniquely identify the steps
                );
                count++;

                if (!result.Contains("Approval workflow initiated", StringComparison.OrdinalIgnoreCase))
                {
                    throw new Exception($"Failed to initiate approval workflow for ClockInOut ID: {record.IdClockDetails}. Error: {result}");
                }
            }
            await _dbContext.SaveChangesAsync();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in batch updating ClockIn/Out entries.");
            return false;
        }
    }


    public async Task<bool> UpdateAttendanceShortTimeDetailsAsync(UpdateShortTimeReasonDto dto)
    {
        try
        {
            var record = await _dbContext.Set<DayAttendance>()
                .FirstOrDefaultAsync(x => x.IdDayAttendance == dto.IdDayAttendance);

            if (record == null)
                return false;

            record.ReasonForShortTime = dto.ReasonForShortTime;

            _dbContext.Update(record);
            await _dbContext.SaveChangesAsync();

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating short time reason for IdDayAttendance {Id}", dto.IdDayAttendance);
            throw;
        }
    }

}
