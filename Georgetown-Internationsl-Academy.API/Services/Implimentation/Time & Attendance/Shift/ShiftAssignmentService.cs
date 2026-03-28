using AutoMapper;
using Dapper;
using Georgetown_International_Academy.API.Database;
using Georgetown_Internationsl_Academy.API.DTO.Time___Attendance.Shift;
using Georgetown_Internationsl_Academy.API.Models.Time___Attendance.Shift;
using Georgetown_Internationsl_Academy.API.Services.Interface;
using Georgetown_Internationsl_Academy.API.Services.Interface.Time___Attendance.Shift;
using Microsoft.EntityFrameworkCore;
using System.Data;
using System.Linq;

namespace Georgetown_Internationsl_Academy.API.Services.Implimentation.Time___Attendance.Shift
{
    public class ShiftAssignmentService : IShiftAssignmentService
    {
        private readonly ApplicationDBContext _dbContext;
        private readonly IMapper _mapper;
        private readonly ILogger<ShiftAssignmentService> _logger;
        private readonly IAuditService _auditService;
        public ShiftAssignmentService(ApplicationDBContext dbContext, IMapper mapper, ILogger<ShiftAssignmentService> logger, IAuditService auditService)
        {
            _dbContext = dbContext;
            _mapper = mapper;
            _logger = logger;
            _auditService = auditService;
        }

        public async Task<List<ShiftAssignmentDto>> GetShiftAssignmentsByShiftAsync(int idShift)
        {
            try
            {
                using var connection = _dbContext.Database.GetDbConnection();

                if (connection.State == ConnectionState.Closed)
                    await connection.OpenAsync();

                var query = @"
                        SELECT 
                            sa.IdShiftAssignment,
                            sa.IdShift,
                            sa.IdEmployee,
                            sa.IdShiftSchedule,
                            sa.StartDate,
                            sa.EndDate,
                            sa.TotalDurationMinutes,
                            sa.TotalDurationHours,
                            sa.AttendanceStatus,
                            s.ShiftName,
                            e.EmployeeCode,
                            CONCAT(e.FirstName, ' ', COALESCE(e.MiddleName, ''), ' ', e.LastName) AS EmployeeName,
                            e.IdDepartment,
                            e.IdDesignation,
                            d.DepartmentName AS Department,
                            des.DesignationName AS Designation
                        FROM ShiftAssignments sa
                        INNER JOIN Employees e ON sa.IdEmployee = e.IdEmployee
                        INNER JOIN ShiftDefinitions s ON sa.IdShift = s.IdShift
                        INNER JOIN Departments d ON e.IdDepartment = d.IdDepartment
                        INNER JOIN Designations des ON e.IdDesignation = des.IdDesignation
                        WHERE sa.IdShift = @IdShift
                        ORDER BY sa.StartDate DESC, e.FirstName";

                return (await connection.QueryAsync<ShiftAssignmentDto>(query, new { IdShift = idShift })).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching shift assignments for IdShift: {IdShift}", idShift);
                throw;
            }
        }

        public async Task<List<ShiftAssignmentDto>> ManageShiftAssignmentsAsync(List<ShiftAssignmentDto> assignments)
        {
            var resultDtos = new List<ShiftAssignmentDto>();

            try
            {
                if (assignments == null || !assignments.Any())
                    return resultDtos;

                var idShiftSchedule = assignments.First().IdShiftSchedule;

                var shiftSchedule = await _dbContext.ShiftSchedules.Where(sa => sa.IdShiftSchedule == idShiftSchedule).FirstOrDefaultAsync();

                // Get existing assignments for the schedule
                var existingAssignments = await _dbContext.ShiftAssignments
                    .Where(sa => sa.IdShiftSchedule == idShiftSchedule)
                    .ToListAsync();

                // Get incoming IDs (i.e., assignments to keep or update)
                var incomingIds = assignments
                    .Where(sa => sa.IdShiftAssignment.HasValue && sa.IdShiftAssignment > 0)
                    .Select(sa => sa.IdShiftAssignment.Value)
                    .ToHashSet();

                // Remove assignments that are not in the incoming list
                var toRemove = existingAssignments
                    .Where(existing => !incomingIds.Contains((int)existing.IdShiftAssignment))
                    .ToList();

                _dbContext.ShiftAssignments.RemoveRange(toRemove);

                foreach (var dto in assignments)
                {
                    var duration = (dto.EndDate - dto.StartDate).Duration();
                    dto.TotalDurationMinutes = (int)duration.TotalMinutes;
                    dto.TotalDurationHours = (decimal)duration.TotalHours;

                    if (dto.IdShiftAssignment.HasValue && dto.IdShiftAssignment > 0)
                    {
                        // Update
                        var existing = await _dbContext.ShiftAssignments.FindAsync(dto.IdShiftAssignment);
                        if (existing != null)
                        {
                            await _auditService.LogAuditAsync(
                            actionType: "Update",
                            entityName: "ShiftAssignment",
                            entityId: (int)existing.IdShiftAssignment,
                            actionDetails: new { before = existing, after = dto });
                            existing.IdShift = dto.IdShift;
                            existing.IdEmployee = dto.IdEmployee;
                            existing.IdShiftSchedule = dto.IdShiftSchedule;
                            existing.StartDate = dto.StartDate.Date.Add(shiftSchedule.StartTime);
                            existing.EndDate = dto.EndDate.Date.Add(shiftSchedule.EndTime);
                            existing.TotalDurationMinutes = dto.TotalDurationMinutes;
                            existing.TotalDurationHours = dto.TotalDurationHours;
                            existing.AttendanceStatus = dto.AttendanceStatus;

                            _dbContext.ShiftAssignments.Update(existing);
                            resultDtos.Add(_mapper.Map<ShiftAssignmentDto>(existing));
                        }
                    }
                    else
                    {
                        // Add new
                        // Ensure we don't attempt to insert an explicit identity value (e.g. 0)
                        dto.IdShiftAssignment = null;

                        var entity = _mapper.Map<ShiftAssignment>(dto);
                        entity.IdShiftAssignment = null;
                        entity.StartDate = dto.StartDate.Date.Add(shiftSchedule.StartTime);
                        entity.EndDate = dto.EndDate.Date.Add(shiftSchedule.EndTime);

                        await _dbContext.ShiftAssignments.AddAsync(entity);

                        // Persist now so we get the identity value from DB before audit logging.
                        await _dbContext.SaveChangesAsync();

                        await _auditService.LogAuditAsync(
                              actionType: "Add",
                              entityName: "ShiftAssignment",
                              entityId: entity.IdShiftAssignment ?? 0,
                              actionDetails: new { after = entity });

                        resultDtos.Add(_mapper.Map<ShiftAssignmentDto>(entity));
                    }
                }

                // If update rows exist, SaveChanges has not been run yet for them; run again safely.
                await _dbContext.SaveChangesAsync();
                return resultDtos;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error managing shift assignments.");
                return new List<ShiftAssignmentDto>();
            }
        }
        public async Task<bool> DeleteShiftAssignment(int IdShiftAssignment, int IdEmployee, int IdLoginnedEmployee)
        {
            var record = await _dbContext.ShiftAssignments
                .FirstOrDefaultAsync(sa => sa.IdShiftAssignment == IdShiftAssignment
                                       && sa.IdEmployee == IdEmployee);

            if (record == null)
            {
                return false; // Not found
            }

            _dbContext.ShiftAssignments.Remove(record);
            await _dbContext.SaveChangesAsync();

            return true; // Success
        }

        public async Task<bool> DeleteShiftAssignmentOfASchedule(int IdShiftSchedule, DateTime ShiftStartDateTime, int loggedInEmployeeId)
        {
            var record = await _dbContext.ShiftAssignments.
                    Where(sa => sa.IdShiftSchedule == IdShiftSchedule && sa.StartDate == ShiftStartDateTime).ToListAsync();

            if (record == null)
            {
                return false; // Not found
            }

            _dbContext.ShiftAssignments.RemoveRange(record);
            await _dbContext.SaveChangesAsync();

            return true; // Success
        }

        public async Task<List<ShiftAssignmentDto>> CopyShiftAssignmentsByDateAsync(int IdShift, DateTime sourceDate, DateTime targetDate)
        {
            var resultDtos = new List<ShiftAssignmentDto>();
            try
            {
                // Get the shift schedule
                var shiftSchedule = await _dbContext.ShiftSchedules
                    .Where(ss => ss.IdShift == IdShift)
                    .FirstOrDefaultAsync();

                if (shiftSchedule == null)
                {
                    _logger.LogWarning("ShiftSchedule with ID {IdShiftSchedule} not found.", IdShift);
                    return resultDtos;
                }

                // Get source assignments (from sourceDate)
                var sourceAssignments = await _dbContext.ShiftAssignments
                    .Where(sa => sa.IdShift == IdShift
                              && sa.StartDate.Date == sourceDate.Date)
                    .ToListAsync();

                if (!sourceAssignments.Any())
                {
                    _logger.LogWarning("No shift assignments found for source date {SourceDate}.", sourceDate.Date);
                    return resultDtos;
                }

                // Get the day name of the target date (e.g. "MONDAY")
                string targetDayName = targetDate.DayOfWeek.ToString().ToUpper();

                // Get all shift schedules for the same IdShift
                // and check which ones are enabled for the target day
                var allShiftSchedules = await _dbContext.ShiftSchedules
                    .Where(ss => ss.IdShift == shiftSchedule.IdShift)
                    .ToListAsync();

                // Build a set of IdShiftSchedule values that are valid for the target day
                var validShiftScheduleIds = allShiftSchedules
                    .Where(ss =>
                        !string.IsNullOrWhiteSpace(ss.WorkDays) &&
                        ss.WorkDays
                            .Split(',', StringSplitOptions.RemoveEmptyEntries)
                            .Select(d => d.Trim().ToUpper())
                            .Contains(targetDayName)
                    )
                    .Select(ss => ss.IdShiftSchedule)
                    .ToHashSet();

                // Filter source assignments — only copy those whose IdShiftSchedule
                // is enabled for the target day
                var assignmentsToCopy = sourceAssignments
                    .Where(sa => validShiftScheduleIds.Contains((int)sa.IdShiftSchedule))
                    .ToList();

                if (!assignmentsToCopy.Any())
                {
                    _logger.LogWarning(
                        "No eligible shift assignments to copy to {TargetDate}. " +
                        "All source shifts are disabled for {DayName}.",
                        targetDate.Date, targetDayName);
                    return resultDtos;
                }

                // Delete existing assignments on the target date
                // Only delete those whose IdShiftSchedule is valid for the target day
                // (avoid deleting shifts that were manually added for that day outside the copy)
                var targetAssignments = await _dbContext.ShiftAssignments
                    .Where(sa => validShiftScheduleIds.Contains((int)sa.IdShiftSchedule)
                              && sa.StartDate.Date == targetDate.Date)
                    .ToListAsync();

                if (targetAssignments.Any())
                {
                    _dbContext.ShiftAssignments.RemoveRange(targetAssignments);
                    await _dbContext.SaveChangesAsync();
                }

                // Copy eligible assignments to target date
                foreach (var source in assignmentsToCopy)
                {
                    // Get the specific shift schedule for this assignment
                    // to apply its correct StartTime and EndTime
                    var sourceSchedule = allShiftSchedules
                        .FirstOrDefault(ss => ss.IdShiftSchedule == source.IdShiftSchedule);

                    if (sourceSchedule == null) continue;

                    var newAssignment = new ShiftAssignment
                    {
                        IdShiftAssignment = null,
                        IdEmployee = source.IdEmployee,
                        IdShift = source.IdShift,
                        IdShiftSchedule = source.IdShiftSchedule,
                        StartDate = targetDate.Date.Add(sourceSchedule.StartTime),
                        EndDate = targetDate.Date.Add(sourceSchedule.EndTime),
                        TotalDurationMinutes = source.TotalDurationMinutes,
                        TotalDurationHours = source.TotalDurationHours,
                        AttendanceStatus = source.AttendanceStatus,
                    };

                    var result = await _dbContext.ShiftAssignments.AddAsync(newAssignment);
                    resultDtos.Add(_mapper.Map<ShiftAssignmentDto>(result.Entity));
                }

                await _dbContext.SaveChangesAsync();

                _logger.LogInformation(
                    "Copied {Count} assignment(s) from {Source} to {Target}. " +
                    "{Skipped} assignment(s) skipped (shift not enabled for {Day}).",
                    resultDtos.Count,
                    sourceDate.Date,
                    targetDate.Date,
                    sourceAssignments.Count - assignmentsToCopy.Count,
                    targetDayName);

                return resultDtos;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error copying shift assignments from {SourceDate} to {TargetDate}.", sourceDate, targetDate);
                return new List<ShiftAssignmentDto>();
            }
        }
    }

}
