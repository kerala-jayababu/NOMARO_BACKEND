using AutoMapper;
using Dapper;
using Georgetown_International_Academy.API.Database;
using Georgetown_Internationsl_Academy.API.DTO;
using Georgetown_Internationsl_Academy.API.Models;
using Georgetown_Internationsl_Academy.API.Services.Interface;
using Microsoft.EntityFrameworkCore;

namespace Georgetown_Internationsl_Academy.API.Services.Implimentation
{
    public class RoleBasedScreenService : IRoleBasedScreenService
    {
        private readonly ApplicationDBContext _dbContext;
        private readonly ILogger<RoleBasedScreenService> _logger;
        private readonly IMapper _mapper;
        public RoleBasedScreenService(ApplicationDBContext dbContext, ILogger<RoleBasedScreenService> logger, IMapper mapper)
        {
            _dbContext = dbContext;
            _logger = logger;
            _mapper = mapper;
        }
        public async Task<List<PayrollScreenDto>> GetAllPayrollScreens()
        {
            try
            {
                var screens = await _dbContext.PayrollScreens.ToListAsync();
                var payrollScreens = screens
                    .Select(screen => new PayrollScreenDto
                    {
                        IdPayrollScreen = screen.IdPayrollScreen,
                        ScreenName = screen.ScreenName,
                        ValidPermissions = screen.ValidPermissions,
                        IdParentPayrollScreen = screen.IdParentPayrollScreen
                    }).Where(x => x.IdParentPayrollScreen == 0).ToList();

                foreach (var screen in payrollScreens)
                {
                    screen.SubMenus = screens.Where(x => x.IdParentPayrollScreen == screen.IdPayrollScreen).ToList();
                }

                return payrollScreens;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching payroll screens.");
                throw;
            }
        }


        public async Task<bool> ManagePayrollScreens(List<PayrollScreenDto> payrollScreens)
        {
            try
            {
                // Fetch all existing screens to check for updates
                var existingScreens = await _dbContext.PayrollScreens.ToListAsync();

                foreach (var screenDto in payrollScreens)
                {
                    var existingScreen = existingScreens
                        .FirstOrDefault(x => x.IdPayrollScreen == screenDto.IdPayrollScreen);

                    if (existingScreen != null)
                    {
                        // Update existing screen
                        existingScreen.ScreenName = screenDto.ScreenName;
                        existingScreen.ValidPermissions = screenDto.ValidPermissions;
                        existingScreen.IdParentPayrollScreen = screenDto.IdParentPayrollScreen;
                        _dbContext.PayrollScreens.Update(existingScreen);
                    }
                    else
                    {
                        // Add new screen
                        var newScreen = new PayrollScreens
                        {
                            ScreenName = screenDto.ScreenName,
                            ValidPermissions = screenDto.ValidPermissions,
                            IdParentPayrollScreen = screenDto.IdParentPayrollScreen
                        };

                        await _dbContext.PayrollScreens.AddAsync(newScreen);
                    }
                }

                // Save changes
                await _dbContext.SaveChangesAsync();

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error managing payroll screens.");
                throw;
            }
        }




        public async Task<List<EmployeePermissionDto>> GetEmployeePermissionsById(int employeeId)
        {
            const string query = @"
        SELECT 
            ep.IdEmployeePermission,
            ep.IdEmployee,
            ep.IdPayrollScreen,
            ep.Permission,
            ps.ScreenName
        FROM 
            EmployeePermissions ep
        left JOIN 
            PayrollScreens ps ON ep.IdPayrollScreen = ps.IdPayrollScreen
        WHERE 
            ep.IdEmployee = @IdEmployee
        ORDER BY 
            ps.OrderNumber;
    ";

            try
            {
                using (var connection = _dbContext.Database.GetDbConnection())
                {
                    if (connection.State == System.Data.ConnectionState.Closed)
                        await connection.OpenAsync();

                    var permissions = await connection.QueryAsync<EmployeePermissionDto>(query, new { IdEmployee = employeeId });
                    return permissions.ToList(); 
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error fetching permissions for Employee ID: {employeeId} using Dapper.");
                throw new Exception($"An error occurred while retrieving permissions for Employee ID: {employeeId}. Please try again later.", ex);
            }
        }





        public async Task<bool> ManageEmployeePermissions(List<EmployeePermissionDto> employeePermissions)
        {
            try
            {
                var employeeId = employeePermissions.FirstOrDefault()?.IdEmployee;

                // Fetch existing permissions for the employee
                var existingPermissions = await _dbContext.EmployeePermissions
                    .Where(x => x.IdEmployee == employeeId)
                    .ToListAsync();

                foreach (var permissionDto in employeePermissions)
                {
                    var existingPermission = existingPermissions
                        .FirstOrDefault(x => x.IdEmployeePermission == permissionDto.IdEmployeePermission);

                    if (existingPermission != null)
                    {
                        // Update existing permission
                        existingPermission.IdPayrollScreen = permissionDto.IdPayrollScreen;
                        existingPermission.Permission = permissionDto.Permission;

                        _dbContext.EmployeePermissions.Update(existingPermission);
                    }
                    else
                    {
                        // Add new permission
                        var newPermission = new EmployeePermissions
                        {
                            IdEmployee = permissionDto.IdEmployee,
                            IdPayrollScreen = permissionDto.IdPayrollScreen,
                            Permission = permissionDto.Permission
                        };

                        await _dbContext.EmployeePermissions.AddAsync(newPermission);
                    }
                }

                // Save changes
                await _dbContext.SaveChangesAsync();

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error managing employee permissions.");
                throw;
            }
        }


        public async Task<List<RoleBasedPermissionDto>> GetRoleBasedPermissionsByDesignationId(int designationId)
        {
            try
            {
                var permissions = await _dbContext.RoleBasedPermissions
                    .Where(x => x.IdDesignation == designationId)
                    .ToListAsync();

                // Map using AutoMapper
                var mappedPermissions = _mapper.Map<List<RoleBasedPermissionDto>>(permissions);

                return mappedPermissions;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching role-based permissions for Designation ID: {DesignationId}", designationId);
                throw;
            }
        }


        public async Task<bool> ManageRoleBasedPermissions(List<RoleBasedPermissionDto> rolePermissions)
        {
            try
            {
                var designationId = rolePermissions.FirstOrDefault()?.IdDesignation;

                // Fetch existing permissions for the designation
                var existingPermissions = await _dbContext.RoleBasedPermissions
                    .Where(x => x.IdDesignation == designationId)
                    .ToListAsync();

                foreach (var permissionDto in rolePermissions)
                {
                    var existingPermission = existingPermissions
                        .FirstOrDefault(x => x.IdRolePermission == permissionDto.IdRolePermission);

                    if (existingPermission != null)
                    {
                        // Update existing permission
                        existingPermission.IdPayrollScreen = permissionDto.IdPayrollScreen;
                        existingPermission.Permission = permissionDto.Permission;

                        _dbContext.RoleBasedPermissions.Update(existingPermission);
                    }
                    else
                    {
                        // Add new permission
                        var newPermission = new RoleBasedPermission
                        {
                            IdDesignation = permissionDto.IdDesignation,
                            IdPayrollScreen = permissionDto.IdPayrollScreen,
                            Permission = permissionDto.Permission
                        };

                        await _dbContext.RoleBasedPermissions.AddAsync(newPermission);
                    }
                }

                // Save changes
                await _dbContext.SaveChangesAsync();

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error managing role-based permissions.");
                throw;
            }
        }






    }

}
