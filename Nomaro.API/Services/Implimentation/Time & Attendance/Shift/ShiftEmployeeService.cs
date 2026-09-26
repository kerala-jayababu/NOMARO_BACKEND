using AutoMapper;
using Dapper;
using Nomaro.API.Database;
using Nomaro.API.DTO.Time___Attendance.Shift;
using Nomaro.API.Models.Time___Attendance.Shift;
using Nomaro.API.Services.Interface.Time___Attendance.Shift;
using Microsoft.EntityFrameworkCore;
using System;
using System.Data;

public class ShiftEmployeeService : IShiftEmployeeService
{
    private readonly ApplicationDBContext _dbContext;
    private readonly IMapper _mapper;
    private readonly ILogger<ShiftEmployeeService> _logger;

    public ShiftEmployeeService(ApplicationDBContext dbContext, IMapper mapper, ILogger<ShiftEmployeeService> logger)
    {
        _dbContext = dbContext;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<List<ShiftEmployeeDto>> GetShiftEmployeesByShiftAsync(int idShift)
    {
        try
        {
            using (var connection = _dbContext.Database.GetDbConnection())
            {
                if (connection.State == ConnectionState.Closed)
                    await connection.OpenAsync();

                var query = @"
            SELECT 
                se.IdShiftEmployee,
                se.IdShift,
                se.IdEmployee,
                s.ShiftName,
                e.EmployeeCode,
                CONCAT(e.FirstName, ' ', COALESCE(e.MiddleName, ''), ' ', e.LastName) AS EmployeeName,
                e.IdDepartment,
                e.IdDesignation,
                d.DepartmentName AS Department,
                des.DesignationName AS Designation
            FROM ShiftEmployees se
            INNER JOIN Employees e ON se.IdEmployee = e.IdEmployee
            INNER JOIN ShiftDefinitions s ON se.IdShift = s.IdShift
            INNER JOIN Departments d ON e.IdDepartment = d.IdDepartment
            INNER JOIN Designations des ON e.IdDesignation = des.IdDesignation
            WHERE se.IdShift = @IdShift Order by e.FirstName";

                var parameters = new DynamicParameters();
                parameters.Add("IdShift", idShift);

                var result = await connection.QueryAsync<ShiftEmployeeDto>(query, parameters);
                return result.ToList();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching shift employees for IdShift: {IdShift}", idShift);
            throw;
        }
    }


    public async Task<List<ShiftEmployeeDto>> ManageShiftEmployeesAsync(List<ShiftEmployeeDto> employees)
    {
        var resultDtos = new List<ShiftEmployeeDto>();

        try
        {
            if (employees == null || !employees.Any())
                return resultDtos;

            // All employees must have the same IdShift
            var idShift = employees.First().IdShift;

            var existing = await _dbContext.ShiftEmployees
                                           .Where(se => se.IdShift == idShift)
                                           .ToListAsync();

            var incomingEmployeeIds = employees.Select(e => e.IdEmployee).ToHashSet();

            // Remove old ones not in the list
            var toRemove = existing.Where(e => !incomingEmployeeIds.Contains(e.IdEmployee)).ToList();
            _dbContext.ShiftEmployees.RemoveRange(toRemove);

            // Add new ones
            foreach (var dto in employees)
            {
                var exists = existing.Any(e => e.IdEmployee == dto.IdEmployee);
                if (!exists)
                {
                    var entity = new ShiftEmployee
                    {
                        IdShift = dto.IdShift,
                        IdEmployee = dto.IdEmployee
                    };

                    var result = await _dbContext.ShiftEmployees.AddAsync(entity);
                    resultDtos.Add(_mapper.Map<ShiftEmployeeDto>(result.Entity));
                }
                else
                {
                    // Return already existing entries too
                    var existingEntity = existing.First(e => e.IdEmployee == dto.IdEmployee);
                    resultDtos.Add(_mapper.Map<ShiftEmployeeDto>(existingEntity));
                }
            }

            await _dbContext.SaveChangesAsync();

            return resultDtos;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error managing shift employees.");
            return new List<ShiftEmployeeDto>();
        }
    }

}

