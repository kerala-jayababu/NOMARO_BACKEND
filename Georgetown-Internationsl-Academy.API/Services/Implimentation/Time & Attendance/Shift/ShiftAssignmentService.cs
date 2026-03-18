using AutoMapper;
using Dapper;
using Georgetown_International_Academy.API.Database;
using Georgetown_Internationsl_Academy.API.DTO.Time___Attendance.Shift;
using Georgetown_Internationsl_Academy.API.Models.Time___Attendance.Shift;
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

        public ShiftAssignmentService(ApplicationDBContext dbContext, IMapper mapper, ILogger<ShiftAssignmentService> logger)
        {
            _dbContext = dbContext;
            _mapper = mapper;
            _logger = logger;
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
ORDER BY sa.StartDate DESC";

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

                        var result = await _dbContext.ShiftAssignments.AddAsync(entity);
                        resultDtos.Add(_mapper.Map<ShiftAssignmentDto>(result.Entity));
                    }
                }

                await _dbContext.SaveChangesAsync();
                return resultDtos;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error managing shift assignments.");
                return new List<ShiftAssignmentDto>();
            }
        }

    }

}
