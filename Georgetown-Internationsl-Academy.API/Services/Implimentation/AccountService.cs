using Georgetown_International_Academy.API.Database;
using Georgetown_Internationsl_Academy.API.DTO;
using Georgetown_Internationsl_Academy.API.Models;
using Georgetown_Internationsl_Academy.API.Services.Interface;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Georgetown_Internationsl_Academy.API.Services.Implimentation
{
    public class AccountService : IAccountService
    {
        private readonly IConfiguration _configuration;
        private readonly ApplicationDBContext _dbContext;
        private readonly ILogger<NotificationConfigService> _logger;
        public AccountService(IConfiguration configuration, ApplicationDBContext dbContext, ILogger<NotificationConfigService> logger)
        {
            _configuration = configuration;
            _dbContext = dbContext;
            _logger = logger;
        }

        public async Task<UserResponseDto> Login(LoginDto login)
        {
            var user = _dbContext.Employees.FirstOrDefault(x => x.EmailID == login.Email);
            if (user == null)
            {
                return null;
            }

            if (login.Password != "Payroll@123")
            {
                return null;
            }

            var designation = await _dbContext.Designations.FirstOrDefaultAsync(x => x.IdDesignation == user.IdDesignation);

            var identity = new ClaimsIdentity(IdentityConstants.ApplicationScheme);
            identity.AddClaim(new Claim(ClaimTypes.Name, user.FirstName+" "+user.LastName));
            identity.AddClaim(new Claim(ClaimTypes.Role, designation.DesignationName));
            identity.AddClaim(new Claim(ClaimTypes.NameIdentifier, user.IdEmployee.ToString()));
            var authClaims = new List<Claim>
            {
            new(ClaimTypes.Name,  user.FirstName+" "+user.LastName),
            new(ClaimTypes.NameIdentifier, user.IdEmployee.ToString()),
            new(ClaimTypes.Email, user.EmailID.ToString()),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };
            var token = GetToken(authClaims);

            string dbPath = user.EmployeePhotoFilePath?.Trim(); // Remove extra spaces if any


            if (!string.IsNullOrEmpty(dbPath) && System.IO.File.Exists(dbPath))
            {
                user.AttachmentBlob = await System.IO.File.ReadAllBytesAsync(dbPath);
            }

            var userResponse = new UserResponseDto
            {
                Name = user.FirstName + " " + user.MiddleName + " "+  user.LastName,
                UserId = user.IdEmployee,
                Role = designation != null ? designation.DesignationName : string.Empty,
                EmployeePhotoFilePath=user.EmployeePhotoFilePath,
                AttachmentBlob = user.AttachmentBlob,
                Email = user.EmailID,
                Token = new JwtSecurityTokenHandler().WriteToken(token)
            };

            return userResponse;
        }

        private JwtSecurityToken GetToken(List<Claim> authClaims)
        {
            var authSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["JwtSettings:Secret"]));

            var token = new JwtSecurityToken(
                _configuration["JwtSettings:ValidIssuer"],
                _configuration["JwtSettings:ValidAudience"],
                expires: DateTime.Now.AddHours(48),
                claims: authClaims,
                signingCredentials: new SigningCredentials(authSigningKey, SecurityAlgorithms.HmacSha256)
            );

            return token;
        }
        private JwtSecurityToken GetTokenForEmail(List<Claim> authClaims)
        {
            var authSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["JwtSettings:Secret"]));

            var token = new JwtSecurityToken(
                issuer: _configuration["JwtSettings:ValidIssuer"],
                audience: _configuration["JwtSettings:ValidAudience"],
                expires: DateTime.Now.AddHours(365),
                claims: authClaims,
                signingCredentials: new SigningCredentials(authSigningKey, SecurityAlgorithms.HmacSha256)
            // No 'expires' set — token will not expire
            );

            return token;
        }




        public async Task<UserResponseDto> LoginForMail(int EmployeeId)
        {
            var user = _dbContext.Employees.FirstOrDefault(x => x.IdEmployee == EmployeeId);
            if (user == null)
            {
                return null;
            }

           

            var designation = await _dbContext.Designations.FirstOrDefaultAsync(x => x.IdDesignation == user.IdDesignation);

            var identity = new ClaimsIdentity(IdentityConstants.ApplicationScheme);
            identity.AddClaim(new Claim(ClaimTypes.Name, user.FirstName + " " + user.LastName));
            identity.AddClaim(new Claim(ClaimTypes.Role, designation.DesignationName));
            identity.AddClaim(new Claim(ClaimTypes.NameIdentifier, user.IdEmployee.ToString()));
            var authClaims = new List<Claim>
            {
            new(ClaimTypes.Name,  user.FirstName+" "+user.LastName),
            new(ClaimTypes.NameIdentifier, user.IdEmployee.ToString()),
            new(ClaimTypes.Email, user.EmailID.ToString()),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };
            var token = GetTokenForEmail(authClaims);

            string dbPath = user.EmployeePhotoFilePath?.Trim(); // Remove extra spaces if any


            if (!string.IsNullOrEmpty(dbPath) && System.IO.File.Exists(dbPath))
            {
                user.AttachmentBlob = await System.IO.File.ReadAllBytesAsync(dbPath);
            }

            var userResponse = new UserResponseDto
            {
                Name = user.FirstName + " " + user.MiddleName + " " + user.LastName,
                UserId = user.IdEmployee,
                Email = user.EmailID,
                Role = designation != null ? designation.DesignationName : string.Empty,
                EmployeePhotoFilePath = user.EmployeePhotoFilePath,
                AttachmentBlob = user.AttachmentBlob,
                Token = new JwtSecurityTokenHandler().WriteToken(token)
            };

            return userResponse;
        }






        public async Task<UserResponseDto> DecryptToken(string token)
        {
            if (string.IsNullOrEmpty(token))
            {
                return null; // Handle invalid input properly
            }
            string[] parts = token.Split(',');
            string pureToken = parts[0];
            string notificationIdStr = parts[1];
            var handler = new JwtSecurityTokenHandler();
            JwtSecurityToken jwtToken;

            try
            {
                jwtToken = handler.ReadJwtToken(pureToken);
            }
            catch (Exception)
            {
                return null; // Handle invalid token
            }
            var notification = await _dbContext.Notifications.Where(x=>x.IdNotification == Convert.ToInt32(notificationIdStr)).FirstOrDefaultAsync();
            if (notification != null)
            {
                notification.IsReadAppNotification = true;
                notification.ReadAt = DateTime.Now;
                await _dbContext.SaveChangesAsync();
            }

            var claims = jwtToken.Claims.ToDictionary(c => c.Type, c => c.Value);

            // Extract email claim safely
            if (!claims.TryGetValue(ClaimTypes.Email, out var email))
            {
                return null; // Email claim not found
            }

            // Fetch user based on email
            var userResponse = await checkUser(email, pureToken);
            return userResponse;
        }


        public async Task<UserResponseDto> checkUser(string EmailID, string token)
        {
            var user = await _dbContext.Employees
                .FirstOrDefaultAsync(x => x.EmailID == EmailID);

            if (user == null) return null;

            var designation = await _dbContext.Designations
                .FirstOrDefaultAsync(x => x.IdDesignation == user.IdDesignation);

            string dbPath = user.EmployeePhotoFilePath?.Trim();

            if (!string.IsNullOrEmpty(dbPath) && System.IO.File.Exists(dbPath))
            {
                user.AttachmentBlob = await System.IO.File.ReadAllBytesAsync(dbPath);
            }

            return new UserResponseDto
            {
                Name = $"{user.FirstName} {user.MiddleName} {user.LastName}",
                UserId = user.IdEmployee,
                Role = designation?.DesignationName ?? string.Empty,
                EmployeePhotoFilePath = user.EmployeePhotoFilePath,
                AttachmentBlob = user.AttachmentBlob,
                Token = token
            };
        }

        public async Task<string> ValidateLogin(string emailId)
        {
            try
            {
                var user = await _dbContext.Employees
                    .FirstOrDefaultAsync(x => x.EmailID == emailId);

                if (user == null) return null;

                var employeePermissions = await _dbContext.EmployeePermissions
                    .Where(x => x.IdEmployee == user.IdEmployee && x.Permission.Length > 0)
                    .ToListAsync();

                if (employeePermissions == null || !employeePermissions.Any())
                    return "SelfPortal";

                return "Payroll,SelfPortal";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating login for email: {EmailId}", emailId);
                throw; // Let the controller handle the error and return proper status code
            }
        }

    }
}
