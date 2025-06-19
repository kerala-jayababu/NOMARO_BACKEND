using AutoMapper;
using Dapper;
using Georgetown_International_Academy.API.Database;
using Georgetown_Internationsl_Academy.API.DTO.Shift;
using Georgetown_Internationsl_Academy.API.DTO.Time___Attendance.Shift;
using Georgetown_Internationsl_Academy.API.Models.Shift;
using Georgetown_Internationsl_Academy.API.Models.Time___Attendance.Shift;
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

    public ShiftService(ApplicationDBContext dbContext, IMapper mapper, ILogger<ShiftService> logger)
    {
        _dbContext = dbContext;
        _mapper = mapper;
        _logger = logger;
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



    public async Task<List<ClockInOutDto>> GetClockInClockOutDetailsAsync(string idEmployeeString, DateTime dateFrom, DateTime dateTo)
    {
        try
        {
            using (var connection = _dbContext.Database.GetDbConnection() as SqlConnection)
            {
                var parameters = new DynamicParameters();
                parameters.Add("@IdEmployeeString", idEmployeeString, DbType.String);
                parameters.Add("@Datefrom", dateFrom, DbType.DateTime);
                parameters.Add("@DateTo", dateTo, DbType.DateTime);

                var result = await connection.QueryAsync<ClockInOutDto>(
                    "GetClockInClockOutDetails",
                    parameters,
                    commandType: CommandType.StoredProcedure);

                return result.ToList();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving clock-in/out details for Employee(s): {EmployeeString}, From: {DateFrom}, To: {DateTo}",
                idEmployeeString, dateFrom, dateTo);

            throw new Exception("An error occurred while retrieving clock-in/out details. Please try again later.");
        }
    }

    public async Task<IEnumerable<DayAttendanceDto>> GetDayAttendanceDetails(
     DateTime? dateFrom = null, DateTime? dateTo = null, int? idEmployee = null, int? idDepartment = null)
    {
        var query = new StringBuilder(@"
        SELECT 
            e.IdEmployee,
            CONCAT(e.FirstName, ' ', ISNULL(e.MiddleName, ''), ' ', e.LastName) AS EmployeeName,
            e.IdDesignation,
            dsg.DesignationName,
            e.IdDepartment,
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
            da.TotalDurationHoursText,
            da.ActualHoursText,
            da.StatusDetails,
            da.ReasonForShortTime,
            da.TimeSheetApprovalStatus,
            da.IdDayAttendance
        FROM DayAttendance da
        INNER JOIN Employees e ON da.IdEmployee = e.IdEmployee
        INNER JOIN Departments dept ON e.IdDepartment = dept.IdDepartment
        INNER JOIN Designations dsg ON e.IdDesignation = dsg.IdDesignation
        WHERE 1 = 1
    ");

        var parameters = new DynamicParameters();

        if (dateFrom.HasValue && dateTo.HasValue)
        {
            query.Append(" AND da.AttendanceDate BETWEEN @DateFrom AND @DateTo");
            parameters.Add("DateFrom", dateFrom);
            parameters.Add("DateTo", dateTo);
        }

        if (idEmployee.HasValue)
        {
            query.Append(" AND da.IdEmployee = @IdEmployee");
            parameters.Add("IdEmployee", idEmployee);
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


    public async Task<bool> ApproveTimesheetAsync(ApproveTimesheetDto dto, int EmployeeId)
    {
        try
        {
            var attendance = await _dbContext.Set<DayAttendance>().FirstOrDefaultAsync(a =>
                a.IdDayAttendance == dto.IdDayAttendance);

            if (attendance == null)
                return false;

            attendance.TimeSheetApprovalStatus = dto.ApprovalStatus;
            attendance.ApprovedDateTime = DateTime.UtcNow;
            attendance.IdApprovedBy = EmployeeId;
            attendance.StatusDetails = dto.RejectReasons;

            _dbContext.Update(attendance);
            await _dbContext.SaveChangesAsync();

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error approving timesheet.");
            throw;
        }
    }

    public async Task<bool> UpdateClockInOutMissingEntryAsync(UpdateClockInOutMissingEntryDto dto)
    {
        try
        {
            var record = await _dbContext.Set<ClockInOutDetails>()
                .FirstOrDefaultAsync(c => c.IdClockDetails == dto.IdClockDetail);

            if (record == null)
                return false;

            if (dto.ClockType == "IN")
                record.INTime = dto.Time;
            else if (dto.ClockType == "OUT")
                record.OUTTime = dto.Time;

            record.Remarks = dto.Reason;
            record.StatusDetails = "Missing-ManualEntry";

            _dbContext.Update(record);
            await _dbContext.SaveChangesAsync();

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating missing ClockIn/Out entry for ClockDetailId {Id}", dto.IdClockDetail);
            throw;
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
