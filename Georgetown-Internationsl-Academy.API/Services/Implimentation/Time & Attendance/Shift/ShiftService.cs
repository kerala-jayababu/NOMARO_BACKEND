using AutoMapper;
using Dapper;
using Georgetown_International_Academy.API.Database;
using Georgetown_Internationsl_Academy.API.DTO;
using Georgetown_Internationsl_Academy.API.DTO.Shift;
using Georgetown_Internationsl_Academy.API.DTO.Time___Attendance.Shift;
using Georgetown_Internationsl_Academy.API.Models;
using Georgetown_Internationsl_Academy.API.Models.Shift;
using Georgetown_Internationsl_Academy.API.Models.Time___Attendance.Shift;
using Georgetown_Internationsl_Academy.API.Services.Interface;
using Georgetown_Internationsl_Academy.API.Services.Interface.Shift;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Org.BouncyCastle.Pqc.Crypto.Lms;
using System.Data;
using System.Text;
using static iTextSharp.text.pdf.AcroFields;

public class ShiftService : IShiftService
{
    private readonly ApplicationDBContext _dbContext;
    private readonly IMapper _mapper;
    private readonly ILogger<ShiftService> _logger;
    private readonly IConfiguration _configuration;
    private readonly IApprovalWorkflowService _approvalWorkflowService;
    private readonly IAuditService _auditService;
    public ShiftService(ApplicationDBContext dbContext, IMapper mapper, ILogger<ShiftService> logger, IConfiguration configuration, IApprovalWorkflowService approvalWorkflowService, IAuditService auditService)
    {
        _dbContext = dbContext;
        _mapper = mapper;
        _logger = logger;
        _configuration = configuration;
        _approvalWorkflowService = approvalWorkflowService;
        _auditService = auditService;
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
            if (shiftDto.IsRegularShiftJustTimeChange == null)
                shiftDto.IsRegularShiftJustTimeChange = false;

            _logger.LogInformation("Adding new shift: {ShiftName}", shiftDto.ShiftName);

            var entity = _mapper.Map<ShiftDefinitionEntity>(shiftDto);
            var result = await _dbContext.ShiftDefinitions.AddAsync(entity);
            await _dbContext.SaveChangesAsync();

            await _auditService.LogAuditAsync(
                actionType: "Add",
                entityName: "ShiftDefinition",
                entityId: entity.IdShift,
                actionDetails: new { after = entity });

            _logger.LogInformation("Added shift ID {ShiftId} name {ShiftName}", entity.IdShift, entity.ShiftName);

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

            var beforeUpdate = _mapper.Map<ShiftDefinitionEntity>(existing);

            existing.ShiftName = shiftDto.ShiftName;
            if (shiftDto.IsRegularShiftJustTimeChange == null)
                existing.IsRegularShiftJustTimeChange = false;

            existing.IsRegularShiftJustTimeChange = shiftDto.IsRegularShiftJustTimeChange;
            _dbContext.ShiftDefinitions.Update(existing);
            await _dbContext.SaveChangesAsync();

            await _auditService.LogAuditAsync(
                actionType: "Update",
                entityName: "ShiftDefinition",
                entityId: existing.IdShift,
                actionDetails: new { before = beforeUpdate, after = existing });

            _logger.LogInformation("Updated shift ID {ShiftId} name {ShiftName}", existing.IdShift, existing.ShiftName);

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
                        TotalInHoursText = c.TotalInHoursText
                                        .Replace(" hrs ", "hr ")
                                        .Replace("minutes", "min"),
                        StatusDetails = c.StatusDetails,
                        Remarks = c.Remarks
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

    public async Task<bool> UpdateClockInOutMissingEntriesAsync(List<UpdateClockInOutMissingEntryDto> dtos, int employeeid)
    {
        try
        {
            var ids = dtos.Select(d => d.IdClockDetail).ToList();

            var records = await _dbContext.Set<ClockInOutDetails>()
                .Where(c => ids.Contains(c.IdClockDetails))
                .ToListAsync();

            if (records.Count != dtos.Count)
                return false; // Some records not found

            // Load all day-records for all involved employees/dates in one go
            var employeeIds = records.Select(r => r.IdEmployee).Distinct().ToList();
            var dates = records.Select(r => r.ClockDate).Distinct().ToList();

            var allDayRecords = await _dbContext.Set<ClockInOutDetails>()
                .Where(c => employeeIds.Contains(c.IdEmployee)
                         && dates.Contains(c.ClockDate))
                .OrderBy(c => c.IdEmployee)
                .ThenBy(c => c.ClockDate)
                .ThenBy(c => c.IdClockDetails)
                .ToListAsync();

            foreach (var dto in dtos)
            {
                var record = records.FirstOrDefault(r => r.IdClockDetails == dto.IdClockDetail);
                if (record == null)
                    return false;

                // All records for this employee+date
                var dayRecords = allDayRecords
                    .Where(c => c.IdEmployee == record.IdEmployee
                             && c.ClockDate == record.ClockDate)
                    .OrderBy(c => c.IdClockDetails)
                    .ToList();

                var index = dayRecords.FindIndex(r => r.IdClockDetails == record.IdClockDetails);
                if (index == -1)
                    throw new Exception("Clock record not found in day records.");

                var previousRecord = index > 0 ? dayRecords[index - 1] : null;
                var nextRecord = index < dayRecords.Count - 1 ? dayRecords[index + 1] : null;

                if (dto.ClockType == "IN")
                {
                    DateTime? previousOut = previousRecord?.OUTTime;
                    DateTime? nextIN = nextRecord?.INTime;
                    DateTime? nextOUT = nextRecord?.OUTTime;

                    // New IN must be earlier than this record's OUT (if exists)
                    if (record.OUTTime.HasValue && dto.Time >= record.OUTTime.Value)
                        throw new Exception("IN time must be earlier than OUT time");

                    // New IN must be after previous OUT (if exists)
                    if (previousOut != null && dto.Time <= previousOut.Value)
                        throw new Exception("IN time must be greater than previous OUT time");

                    // New IN must be earlier than next IN (if exists)
                    if (nextIN != null && dto.Time >= nextIN.Value)
                        throw new Exception("IN time must be earlier than next IN time");

                    // Optionally, earlier than next OUT (if exists)
                    if (nextOUT != null && dto.Time >= nextOUT.Value)
                        throw new Exception("IN time must be earlier than next OUT time");

                    record.INTime = dto.Time;
                    record.StatusDetails = "Missing-ManualInEntry";

                }
                else if (dto.ClockType == "OUT")
                {
                    DateTime? previousOUT = previousRecord?.OUTTime;
                    DateTime? nextIN = nextRecord?.INTime;

                    if (!record.INTime.HasValue)
                        throw new Exception("OUT time cannot be set before IN time");

                    // OUT must be later than its own IN
                    if (dto.Time <= record.INTime.Value)
                        throw new Exception("OUT time must be later than IN time");

                    // OUT must be later than previous OUT (if exists)
                    if (previousOUT != null && dto.Time <= previousOUT.Value)
                        throw new Exception("OUT time must be greater than previous OUT time");

                    // OUT must be earlier than next IN (if exists)
                    if (nextIN != null && dto.Time >= nextIN.Value)
                        throw new Exception("OUT time must be earlier than next IN time");

                    record.OUTTime = dto.Time;
                    record.StatusDetails = "Missing-ManualOutEntry";


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

                record.Remarks = dto.Reason;

            }

            _dbContext.UpdateRange(records);

            var entityCode = _configuration["WorkflowEntityCodes:MissingEntry"];
            int count = 1;

            foreach (var record in records)
            {
                var result = await _approvalWorkflowService.InitiateApprovalWorkflow(
                    record.IdClockDetails,
                    entityCode,
                    employeeid,
                    "SUBMITTED",
                    null,
                    null,
                    count);

                count++;

                if (!result.Contains("Approval workflow initiated", StringComparison.OrdinalIgnoreCase))
                {
                    throw new Exception(
                        $"Failed to initiate approval workflow for ClockInOut ID: {record.IdClockDetails}. Error: {result}");
                }
            }

            await _dbContext.SaveChangesAsync();
            foreach(var updattendance in dtos)
            {
                if (updattendance.ClockType== "OUT")
                {

                    await _dbContext.Database.ExecuteSqlRawAsync(
                        "EXEC UpdateDayAttendanceEmployee @AttendanceDate, @IdEmployee",
                        new SqlParameter("@AttendanceDate", updattendance.Time.Date),
                        new SqlParameter("@IdEmployee", updattendance.IdEmployee)
                    );

                    await _dbContext.SaveChangesAsync();
                }
            }
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

    public async Task<List<MissingEntryForApprovalDto>> GetMissingEntryDetailsForApproval(int IdLoginnedEmployee, DateTime? dateFrom,
         string? approvalStatus)
    {
        var query =
            from ci in _dbContext.ClockInOutDetails
            join af in _dbContext.ApprovalWorkFlowAllocations
                on ci.IdClockDetails equals af.EntityTablePrimaryKeyID
            join emp in _dbContext.Employees
                on ci.IdEmployee equals emp.IdEmployee
            join desig in _dbContext.Designations
                on emp.IdDesignation equals desig.IdDesignation
            join dept in _dbContext.Departments
                on emp.IdDepartment equals dept.IdDepartment
            where af.EntityCode == "MISSINGENTRY"
                  && af.TargetIdEmployee == IdLoginnedEmployee.ToString()
            select new { ci, emp, desig, dept };

        // Apply conditional filters
        if (dateFrom.HasValue)
        {
            query = query.Where(x => x.ci.ClockDate >= dateFrom.Value);
        }

        if (!string.IsNullOrEmpty(approvalStatus))
        {
            query = query.Where(x => x.ci.MissingEntryApproveStatus == approvalStatus);
        }

        var result = await query
            .Select(x => new MissingEntryForApprovalDto
            {
                IdClockInDetail = x.ci.IdClockDetails,
                Idemployee = x.emp.IdEmployee.Value,
                EmployeeName = (x.emp.FirstName ?? "") + " " + (x.emp.LastName ?? ""),
                DesignationName = x.desig.DesignationName,
                DepartmentName = x.dept.DepartmentName,
                MissingEntryDate = x.ci.ClockDate,

                MissingManualEntryTime =
                    x.ci.StatusDetails != null && x.ci.StatusDetails.Contains("ManualIn")
                        ? x.ci.INTime
                        : x.ci.OUTTime,

                ManualEntryType =
                    x.ci.StatusDetails != null && x.ci.StatusDetails.Contains("ManualIn")
                        ? "IN"
                        : "OUT",

                Reason = x.ci.Remarks,
                ApprovalStatus = x.ci.MissingEntryApproveStatus,
                StatusDetails = x.ci.StatusDetails
            })
            .AsNoTracking()
            .ToListAsync();

        return result;
    }

    public async Task<bool> TogglingMissingEntry(int IdClockInDetail)
    {
        try
        {
            var record = await _dbContext.ClockInOutDetails.Where(c => c.IdClockDetails == IdClockInDetail).FirstOrDefaultAsync();
            if (record == null)
                throw new Exception("Record not found");

            _dbContext.Update(record);
            await _dbContext.SaveChangesAsync();
            if(record.INTime == null)
            {
                record.INTime = record.OUTTime;
                record.OUTTime = null;
                record.Remarks = "Entry Toggled from OUT to IN";
            }
            else
            {
                record.OUTTime = record.INTime;
                record.INTime = null;
                record.Remarks = "Entry Toggled from IN to OUT";
            }
            await _dbContext.SaveChangesAsync();

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in Toggling the Clock Details");
            throw;
        }

    }

    public async Task<bool> ForgotAccessCardMissingEntry(ForgotAccessCardMissingEntryDto entryDetails, int IdLogginedEmployee)
    {
        try
        {
            string recordStatus = string.Empty;

            var guyanaTimeZone = TimeZoneInfo.FindSystemTimeZoneById("America/Guyana");
            var currentGuyanaTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, guyanaTimeZone);

            var sameDayRecords = await _dbContext.ClockInOutDetails
                .Where(c => c.ClockDate == entryDetails.EntryDate.Date
                         && c.IdEmployee == entryDetails.IdEmployee)
                .ToListAsync();

            if (entryDetails.EntryDate.Date != currentGuyanaTime.Date)
            {
                throw new ArgumentException(
                    $"You can enter Forgot Card details only for today"
                );
            }

            if (entryDetails.EntryTime > currentGuyanaTime.AddMinutes(1))
            {
                throw new ArgumentException(
                    $"Invalid Time. Entry time must be less than current time"
                );
            }
            if (sameDayRecords.Count > 1)
            {
                throw new ArgumentException(
                    $"Multiple clock-in/out records already exist. Forgot card entry is not allowed."
                );
            }

            var existingRecord = sameDayRecords.FirstOrDefault();

            if (existingRecord == null)
            {
                if (entryDetails.EntryType != "IN")
                    throw new ArgumentException("Please enter IN time first.");

                recordStatus = "IN";
            }
            else
            {
                if (existingRecord.StatusDetails == "IN and OUT Recorded")
                {
                    throw new ArgumentException(
                        $"Attendance already completed"
                    );
                }

                if (entryDetails.EntryType != "OUT")
                {
                    throw new ArgumentException(
                        $"IN time already recorded at {existingRecord.INTime:HH:mm}. Please submit OUT time."
                    );
                }

                if (existingRecord.OUTTime != null)
                {
                    throw new ArgumentException(
                        $"OUT time already recorded at {existingRecord.OUTTime:HH:mm}. Duplicate OUT entry is not allowed."
                    );
                }

                recordStatus = "OUT";
            }

            if (recordStatus == "IN")
            {
                var newRecord = new ClockInOutDetails
                {
                    IdEmployee = entryDetails.IdEmployee,
                    ClockDate = entryDetails.EntryTime.Date,
                    INTime = entryDetails.EntryTime,
                    DeviceUser = "MANUAL",
                    StatusDetails = "IN Punch Recorded",
                    Remarks = "Forgot Access Card: " + entryDetails.Reason.Replace("Forgot Access Card: ","").Trim(),
                    MissingEntryApproveStatus = "SUBMITTED"
                };

                _dbContext.ClockInOutDetails.Add(newRecord);
            }
            else
            {
                existingRecord.OUTTime = entryDetails.EntryTime;
                existingRecord.StatusDetails = "IN and OUT Recorded";
                existingRecord.Remarks = "Forgot Access Card: " + entryDetails.Reason;
                existingRecord.MissingEntryApproveStatus = "SUBMITTED";

                if (existingRecord.INTime.HasValue)
                {
                    var totalMinutes = (int)(existingRecord.OUTTime.Value - existingRecord.INTime.Value).TotalMinutes;

                    if (totalMinutes < 0)
                    {
                        throw new ArgumentException(
                            $"Invalid time sequence. OUT time ({existingRecord.OUTTime:HH:mm}) cannot be earlier than IN time ({existingRecord.INTime:HH:mm})."
                        );
                    }

                    existingRecord.TotalInMinutes = totalMinutes;
                    existingRecord.TotalINHours = Math.Round((decimal)totalMinutes / 60, 2);

                    int hours = totalMinutes / 60;
                    int minutes = totalMinutes % 60;

                    existingRecord.TotalInHoursText = $"{hours}hrs {minutes}minutes";
                    var result = await _approvalWorkflowService.InitiateApprovalWorkflow(
                            existingRecord.IdClockDetails, "FORGOTCARD", IdLogginedEmployee, "SUBMITTED", null,null,1);

                    if (!result.Contains("Approval workflow initiated", StringComparison.OrdinalIgnoreCase))
                    {
                        throw new Exception(
                            $"Failed to initiate approval workflow");
                    }
                }
            }
          
            await _dbContext.SaveChangesAsync();
            return true;
        }
        catch (ArgumentException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in ForgotAccessCardMissingEntry");
            throw;
        }
    }

    public async Task<List<ForgotCardEntryForApprovalDto>> GetForgotCardEntryDetailsForApproval(int IdLoginnedEmployee, DateTime? dateFrom,
         string? approvalStatus)
    {
        var query =
            from ci in _dbContext.ClockInOutDetails
            join af in _dbContext.ApprovalWorkFlowAllocations
                on ci.IdClockDetails equals af.EntityTablePrimaryKeyID
            join emp in _dbContext.Employees
                on ci.IdEmployee equals emp.IdEmployee
            join desig in _dbContext.Designations
                on emp.IdDesignation equals desig.IdDesignation
            join dept in _dbContext.Departments
                on emp.IdDepartment equals dept.IdDepartment
            where af.EntityCode == "FORGOTCARD"
                  && af.TargetIdEmployee == IdLoginnedEmployee.ToString()
            select new { ci, emp, desig, dept };

        // Apply conditional filters
        if (dateFrom.HasValue)
        {
            query = query.Where(x => x.ci.ClockDate >= dateFrom.Value);
        }

        if (!string.IsNullOrEmpty(approvalStatus))
        {
            query = query.Where(x => x.ci.MissingEntryApproveStatus == approvalStatus);
        }

        var result = await query
            .Select(x => new ForgotCardEntryForApprovalDto
            {
                IdClockInDetail = x.ci.IdClockDetails,
                Idemployee = x.emp.IdEmployee.Value,
                EmployeeName = (x.emp.FirstName ?? "") + " " + (x.emp.LastName ?? ""),
                DesignationName = x.desig.DesignationName,
                DepartmentName = x.dept.DepartmentName,
                ForgotCardEntryDate = x.ci.ClockDate,
                EntryTime = x.ci.INTime,
                ExitTime = x.ci.OUTTime,
                Reason = x.ci.Remarks,
                ApprovalStatus = x.ci.MissingEntryApproveStatus,
                StatusDetails = x.ci.StatusDetails
            })
            .AsNoTracking()
            .ToListAsync();

        return result;
    }
}
