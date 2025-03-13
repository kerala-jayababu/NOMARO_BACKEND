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
        public async Task<List<PayrollScreenDto>> GetAllPayrollScreens(int idEmployee)
        {
            try
            {
                var employeePermissions = await _dbContext.EmployeePermissions.Where(x => x.IdEmployee == idEmployee).ToListAsync();
                var screens = await _dbContext.PayrollScreens.Where(x=>x.Enabled==true).ToListAsync();

                var payrollScreens = screens
                    .Select(screen => new PayrollScreenDto
                    {
                        IdPayrollScreen = screen.IdPayrollScreen,
                        ScreenName = screen.ScreenName,
                        ValidPermissions = screen.ValidPermissions,
                        IdParentPayrollScreen = screen.IdParentPayrollScreen,
                        OrderNumber =screen.OrderNumber

                    }).OrderBy(x => x.OrderNumber).Where(x => x.IdParentPayrollScreen == 0).ToList();

                foreach (var screen in payrollScreens)
                {
                    var permission = employeePermissions.FirstOrDefault(x => x.IdPayrollScreen == screen.IdPayrollScreen);
                    if(permission != null)
                    {
                        screen.ValidPermissions = permission.Permission;
                    }

                    var SubMenus = screens.Where(x => x.IdParentPayrollScreen == screen.IdPayrollScreen).ToList();

                    foreach (var subMenu in SubMenus)
                    {
                        var subMenuPermission = employeePermissions.FirstOrDefault(x => x.IdPayrollScreen == subMenu.IdPayrollScreen);
                        if (subMenuPermission != null)
                        {
                            subMenu.ValidPermissions = subMenuPermission.Permission;
                        }
                    }
                    screen.SubMenus = SubMenus;
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
                        existingScreen.ValidPermissions = screenDto.ValidPermissions;
                        //existingScreen.IdParentPayrollScreen = screenDto.IdParentPayrollScreen;
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



        public async Task<List<PayrollScreenDto>> GetEmployeePermissionsById(int employeeId)
        {
            // Fetch payroll screens ordered by OrderNumber
            var screens = await _dbContext.PayrollScreens
                .OrderBy(s => s.OrderNumber) // 🔹 Order by OrderNumber
                .ToListAsync();

            // Fetch employee-specific permissions
            var employeePermissions = await _dbContext.EmployeePermissions
                .Where(ep => ep.IdEmployee == employeeId) // 🔹 Filter by Employee ID
                .ToListAsync();

            // Build the list with employee-specific permissions
            var payrollScreens = screens
                .Select(screen =>
                {
                    var permissionRecord = employeePermissions.FirstOrDefault(ep => ep.IdPayrollScreen == screen.IdPayrollScreen);

                    return new PayrollScreenDto
                    {
                        IdPayrollScreen = screen.IdPayrollScreen,
                        ScreenName = screen.ScreenName,
                        ValidPermissions = permissionRecord?.Permission, // Assign Permission or null
                        IdEmployeePermission = permissionRecord?.IdEmployeePermission ?? 0, // Assign IdEmployeePermission or 0
                        IdParentPayrollScreen = screen.IdParentPayrollScreen
                    };
                })
                .Where(x => x.IdParentPayrollScreen == 0) // 🔹 Filter only parent screens
                .OrderBy(x => x.IdPayrollScreen) // 🔹 Ensure parent menus are also ordered
                .ToList();

            // Build menu hierarchy with ordered submenus
            foreach (var screen in payrollScreens)
            {
                screen.SubMenus = screens
                    .Where(x => x.IdParentPayrollScreen == screen.IdPayrollScreen)
                    .OrderBy(x => x.OrderNumber) // 🔹 Order submenus
                    .Select(subScreen =>
                    {
                        var subPermissionRecord = employeePermissions.FirstOrDefault(ep => ep.IdPayrollScreen == subScreen.IdPayrollScreen);

                        return new PayrollScreens // 🔹 Convert to PayrollScreens while including IdEmployeePermission
                        {
                            IdPayrollScreen = subScreen.IdPayrollScreen,
                            ScreenName = subScreen.ScreenName,
                            ValidPermissions = subPermissionRecord?.Permission, // Assign Permission or null
                            IdEmployeePermission = subPermissionRecord?.IdEmployeePermission ?? 0, // Assign IdEmployeePermission or 0
                            IdParentPayrollScreen = subScreen.IdParentPayrollScreen
                        };
                    })
                    .ToList();
            }

            return payrollScreens;
        }





        public async Task<EmployeePermissions> GetEmployeePermission(int employeeId,int iDPayRollScreen)
        {
            try
            {
                // Fetch all existing screens to check for updates
                var existingScreens = await _dbContext.EmployeePermissions.Where(c=>c.IdPayrollScreen == iDPayRollScreen).FirstOrDefaultAsync();  
                return existingScreens;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error managing payroll screens.");
                throw;
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


        public async Task<List<RoleBasedScreenDto>> GetRoleBasedPermissionsByDesignationId(int designationId)
        {
            // Fetch payroll screens ordered by OrderNumber
            var screens = await _dbContext.PayrollScreens
                .OrderBy(s => s.OrderNumber) // 🔹 Order main and submenu screens
                .ToListAsync();

            // Fetch role-based permissions for the given designation
            var roleBasedPermissions = await _dbContext.RoleBasedPermissions
                .Where(ep => ep.IdDesignation == designationId) // 🔹 Filter by Designation ID
                .ToListAsync();

            // Build parent menus (screens with no parent)
            var payrollScreens = screens
                .Where(x => x.IdParentPayrollScreen == 0) // 🔹 Get only parent screens
                .Select(screen =>
                {
                    var permissionRecord = roleBasedPermissions.FirstOrDefault(ep => ep.IdPayrollScreen == screen.IdPayrollScreen);

                    return new RoleBasedScreenDto
                    {
                        IdPayrollScreen = screen.IdPayrollScreen,
                        ScreenName = screen.ScreenName,
                        ValidPermissions = permissionRecord?.Permission, // Assign Permission or null
                        IdRolePermission = permissionRecord?.IdRolePermission ?? 0, // Assign Role Permission or 0
                        IdParentPayrollScreen = screen.IdParentPayrollScreen,
                        SubMenus = new List<PayrollScreens>() // 🔹 Initialize SubMenus
                    };
                })
                .ToList();

            // Build submenu hierarchy with ordered submenus
            foreach (var screen in payrollScreens)
            {
                screen.SubMenus = screens
                    .Where(x => x.IdParentPayrollScreen == screen.IdPayrollScreen)
                    .OrderBy(x => x.OrderNumber) // 🔹 Order submenus correctly
                    .Select(subScreen =>
                    {
                        var subPermissionRecord = roleBasedPermissions.FirstOrDefault(ep => ep.IdPayrollScreen == subScreen.IdPayrollScreen);

                        return new PayrollScreens // 🔹 Convert to PayrollScreens while including IdRolePermission
                        {
                            IdPayrollScreen = subScreen.IdPayrollScreen,
                            ScreenName = subScreen.ScreenName,
                            ValidPermissions = subPermissionRecord?.Permission, // Assign Permission or null
                            IdRolePermission = subPermissionRecord?.IdRolePermission ?? 0, // Assign IdRolePermission or 0
                            IdParentPayrollScreen = subScreen.IdParentPayrollScreen
                        };
                    })
                    .ToList();
            }

            return payrollScreens;
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
