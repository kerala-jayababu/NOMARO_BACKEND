using Nomaro.API.Database;
using Nomaro.API.DTO;
using Nomaro.API.Services.Interface;
using Microsoft.EntityFrameworkCore;

namespace Nomaro.API.Services.Implimentation
{
    public class ShiftSetupService : IShiftSetupService
    {
        private readonly ApplicationDBContext _dbContext;
        private readonly ILogger<ShiftSetupService> _logger;
        private readonly IConfiguration _configuration;

        public ShiftSetupService(
            ApplicationDBContext dbContext,
            ILogger<ShiftSetupService> logger,
            IConfiguration configuration)
        {
            _dbContext = dbContext;
            _logger = logger;
            _configuration = configuration;
        }

        public async Task<IEnumerable<ShiftSetupOfficeDto>> GetShiftSetupDetails(int employeeId)
        {
            try
            {
                var employee = await _dbContext.Employees
                    .AsNoTracking()
                    .Where(x => x.IdEmployee == employeeId)
                    .Select(x => new
                    {
                        x.IdOffice,
                        DesignationCode = _dbContext.Designations
                            .Where(d => d.IdDesignation == x.IdDesignation)
                            .Select(d => d.DesignationCode)
                            .FirstOrDefault()
                    })
                    .FirstOrDefaultAsync();

                if (employee == null)
                {
                    throw new InvalidOperationException($"Employee {employeeId} was not found.");
                }

                var offices = await (
                    from office in _dbContext.Offices.AsNoTracking()
                    join officeType in _dbContext.OfficeTypes.AsNoTracking()
                        on office.IdOfficeType equals officeType.IdOfficeType
                    join manager in _dbContext.Employees.AsNoTracking()
                        on office.ShiftManager equals manager.IdEmployee into managers
                    from manager in managers.DefaultIfEmpty()
                    select new ShiftSetupOfficeDto
                    {
                        IdOffice = office.IdOffice,
                        OfficeCode = office.OfficeCode,
                        OfficeName = office.OfficeName,
                        IdParentOffice = office.IdParentOffice,
                        Level = officeType.HierarchyLevel,
                        ShiftManager = office.ShiftManager,
                        ShiftManagerName = manager == null
                            ? null
                            : manager.FirstName
                                + (manager.MiddleName != null && manager.MiddleName != "" ? " " + manager.MiddleName : "")
                                + (manager.LastName != null && manager.LastName != "" ? " " + manager.LastName : "")
                    })
                    .OrderBy(x => x.Level)
                    .ThenBy(x => x.OfficeName)
                    .ToListAsync();

                var requiredDesignationCode = _configuration["Designations:Code"] ?? "HRD";
                if (!string.IsNullOrWhiteSpace(requiredDesignationCode)
                    && string.Equals(
                        employee.DesignationCode?.Trim(),
                        requiredDesignationCode.Trim(),
                        StringComparison.OrdinalIgnoreCase))
                {
                    return offices;
                }

                if (!employee.IdOffice.HasValue)
                {
                    return Enumerable.Empty<ShiftSetupOfficeDto>();
                }

                return GetOfficeSubtree(offices, employee.IdOffice.Value);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching shift setup hierarchy for Employee ID: {EmployeeId}", employeeId);
                throw;
            }
        }

        public async Task<IEnumerable<ShiftSetupOfficeDto>> GetShiftSetupHierarchyByOffice(int employeeId, int officeId)
        {
            try
            {
                var visibleOffices = (await GetShiftSetupDetails(employeeId)).ToList();
                if (!visibleOffices.Any(x => x.IdOffice == officeId))
                {
                    throw new UnauthorizedAccessException($"Office {officeId} is outside the employee's office hierarchy.");
                }

                return GetOfficeSubtree(visibleOffices, officeId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching shift setup hierarchy for Office ID: {OfficeId} and Employee ID: {EmployeeId}", officeId, employeeId);
                throw;
            }
        }

        public async Task<IEnumerable<ShiftManagerEmployeeDto>> GetShiftManagerEmployees(int employeeId, int officeId)
        {
            try
            {
                var visibleOffices = await GetShiftSetupDetails(employeeId);
                if (!visibleOffices.Any(x => x.IdOffice == officeId))
                {
                    throw new UnauthorizedAccessException($"Office {officeId} is outside the employee's office hierarchy.");
                }

                return await _dbContext.Employees
                    .AsNoTracking()
                    .Where(x => x.IdOffice == officeId && x.CurrentStatus == "Working")
                    .OrderBy(x => x.FirstName)
                    .ThenBy(x => x.LastName)
                    .Select(x => new ShiftManagerEmployeeDto
                    {
                        IdEmployee = x.IdEmployee!.Value,
                        EmployeeName = x.FirstName
                            + (x.MiddleName != null && x.MiddleName != "" ? " " + x.MiddleName : "")
                            + (x.LastName != null && x.LastName != "" ? " " + x.LastName : ""),
                        Contact = x.PhoneNumber1 ?? x.PhoneNumber2 ?? x.WhatsAppNumber
                    })
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching active shift manager candidates for Office ID: {OfficeId}", officeId);
                throw;
            }
        }

        public async Task UpdateShiftManagerAssignments(int employeeId, List<ShiftManagerAssignmentDto> assignments)
        {
            if (assignments == null || assignments.Count == 0)
            {
                throw new ArgumentException("At least one office assignment is required.");
            }

            if (assignments.Any(x => x.IdOffice <= 0)
                || assignments.Select(x => x.IdOffice).Distinct().Count() != assignments.Count)
            {
                throw new ArgumentException("Office IDs must be valid and unique.");
            }

            var visibleOfficeIds = (await GetShiftSetupDetails(employeeId))
                .Select(x => x.IdOffice)
                .ToHashSet();
            if (assignments.Any(x => !visibleOfficeIds.Contains(x.IdOffice)))
            {
                throw new UnauthorizedAccessException("One or more offices are outside the employee's office hierarchy.");
            }

            var officeIds = assignments.Select(x => x.IdOffice).ToList();
            var officeEntities = await _dbContext.Offices
                .Where(x => officeIds.Contains(x.IdOffice))
                .ToDictionaryAsync(x => x.IdOffice);
            if (officeEntities.Count != assignments.Count)
            {
                throw new InvalidOperationException("One or more offices could not be found.");
            }

            foreach (var assignment in assignments.Where(x => x.IdEmployee.HasValue))
            {
                var isEligibleEmployee = await _dbContext.Employees
                    .AsNoTracking()
                    .AnyAsync(x =>
                        x.IdEmployee == assignment.IdEmployee
                        && x.IdOffice == assignment.IdOffice
                        && x.CurrentStatus == "Working");

                if (!isEligibleEmployee)
                {
                    throw new InvalidOperationException(
                        $"Employee {assignment.IdEmployee} is not an active employee of office {assignment.IdOffice}.");
                }
            }

            foreach (var assignment in assignments)
            {
                var office = officeEntities[assignment.IdOffice];
                office.ShiftManager = assignment.IdEmployee;
                office.IdModifiedBy = employeeId;
                office.ModifiedDateTime = DateTime.Now;
            }

            await _dbContext.SaveChangesAsync();
        }

        private static IEnumerable<ShiftSetupOfficeDto> GetOfficeSubtree(
            List<ShiftSetupOfficeDto> offices,
            int rootOfficeId)
        {
            var childOfficeIdsByParent = offices
                .Where(x => x.IdParentOffice.HasValue)
                .GroupBy(x => x.IdParentOffice!.Value)
                .ToDictionary(x => x.Key, x => x.Select(office => office.IdOffice).ToList());
            var visibleOfficeIds = new HashSet<int> { rootOfficeId };
            var officesToVisit = new Queue<int>();
            officesToVisit.Enqueue(rootOfficeId);

            while (officesToVisit.Count > 0)
            {
                var parentOfficeId = officesToVisit.Dequeue();
                if (!childOfficeIdsByParent.TryGetValue(parentOfficeId, out var childOfficeIds))
                {
                    continue;
                }

                foreach (var childOfficeId in childOfficeIds)
                {
                    if (visibleOfficeIds.Add(childOfficeId))
                    {
                        officesToVisit.Enqueue(childOfficeId);
                    }
                }
            }

            return offices.Where(x => visibleOfficeIds.Contains(x.IdOffice)).ToList();
        }
    }
}