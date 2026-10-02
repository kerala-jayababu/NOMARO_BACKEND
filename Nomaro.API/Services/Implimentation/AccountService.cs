using Nomaro.API.Database;
using Nomaro.API.DTO;
using Nomaro.API.Models;
using Nomaro.API.Services.Interface;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Nomaro.API.Services.Implimentation
{
    public class AccountService : IAccountService
    {
        private const string LegacyPassword = "Payroll@123";
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
            var email = login?.Email?.Trim();
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrEmpty(login?.Password))
            {
                return null;
            }

            var user = await _dbContext.Employees.FirstOrDefaultAsync(x => x.EmailID == email);
            if (user == null)
            {
                return null;
            }

            var hasPasswordHash = !string.IsNullOrEmpty(user.PasswordHash);
            var passwordIsValid = hasPasswordHash
                && _passwordHasher.VerifyHashedPassword(user, user.PasswordHash!, login.Password)
                    != PasswordVerificationResult.Failed;
            var legacyPasswordIsValid = !hasPasswordHash
                && string.Equals(login.Password, LegacyPassword, StringComparison.Ordinal);

            if (!passwordIsValid && !legacyPasswordIsValid)
            {
                return null;
            }

            if (legacyPasswordIsValid)
            {
                user.PasswordHash = _passwordHasher.HashPassword(user, login.Password);
                user.PasswordUpdatedOn = DateTime.Now;
                await _dbContext.SaveChangesAsync();
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
                UserId = (int)user.IdEmployee,
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
                UserId = (int)user.IdEmployee,
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
            var link = await _dbContext.EmployeePermissions
                  .AnyAsync(ep => ep.IdEmployee == user.IdEmployee)
                  ? "PAYROLL,SelfPortal"
                  : "SelfPortal";

            return new UserResponseDto
            {
                Name = $"{user.FirstName} {user.MiddleName} {user.LastName}",
                IdEmployee = user.IdEmployee,
                UserId = (int)user.IdEmployee,
                Role = designation?.DesignationName ?? string.Empty,
                EmployeePhotoFilePath = user.EmployeePhotoFilePath,
                AttachmentBlob = user.AttachmentBlob,
                AuthorizedModules = link,
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

                return "Payroll,SELFPORTAL";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating login for email: {EmailId}", emailId);
                throw; // Let the controller handle the error and return proper status code
            }
        }

        // ───────────── Password login ─────────────

        private const int MaxFailedLoginAttempts = 5;
        private const int LockoutMinutes = 15;
        private const int ResetOtpValidMinutes = 10;
        private const string InvalidCredentialsMessage = "Invalid Email ID or Password.";
        private static readonly PasswordHasher<Employee> _passwordHasher = new PasswordHasher<Employee>();

        public async Task<OTPStatusDto> LoginWithPassword(PasswordLoginDto login)
        {
            var email = login?.EmailID?.Trim();
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrEmpty(login?.Password))
                throw new InvalidOperationException("Enter Email ID and Password.");

            var user = await _dbContext.Employees.FirstOrDefaultAsync(x => x.EmailID == email);
            if (user == null || user.CurrentStatus != "Working")
                throw new InvalidOperationException(InvalidCredentialsMessage);

            if (user.LockoutEndTime.HasValue && user.LockoutEndTime.Value > DateTime.Now)
            {
                var minutesLeft = (int)Math.Ceiling((user.LockoutEndTime.Value - DateTime.Now).TotalMinutes);
                throw new InvalidOperationException(
                    $"Account is locked after too many failed attempts. Try again in {minutesLeft} minute(s) or login using OTP.");
            }

            if (string.IsNullOrEmpty(user.PasswordHash))
            {
                if (!string.Equals(login.Password, LegacyPassword, StringComparison.Ordinal))
                {
                    throw new InvalidOperationException(
                        "Password is not set for this account. Login using OTP, then set a password from Change Password.");
                }

                user.PasswordHash = _passwordHasher.HashPassword(user, login.Password!);
                user.PasswordUpdatedOn = DateTime.Now;
            }
            else
            {
                var result = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, login!.Password!);
                if (result == PasswordVerificationResult.Failed)
                {
                    user.FailedLoginAttempts += 1;
                    if (user.FailedLoginAttempts >= MaxFailedLoginAttempts)
                    {
                        user.FailedLoginAttempts = 0;
                        user.LockoutEndTime = DateTime.Now.AddMinutes(LockoutMinutes);
                        await _dbContext.SaveChangesAsync();
                        _logger.LogWarning("Password login locked for employee {IdEmployee}", user.IdEmployee);
                        throw new InvalidOperationException(
                            $"Too many failed attempts. Password login is locked for {LockoutMinutes} minutes. You can still login using OTP.");
                    }

                    await _dbContext.SaveChangesAsync();
                    var attemptsLeft = MaxFailedLoginAttempts - user.FailedLoginAttempts;
                    throw new InvalidOperationException($"{InvalidCredentialsMessage} {attemptsLeft} attempt(s) left.");
                }

                if (result == PasswordVerificationResult.SuccessRehashNeeded)
                    user.PasswordHash = _passwordHasher.HashPassword(user, login.Password!);
            }

            user.FailedLoginAttempts = 0;
            user.LockoutEndTime = null;
            await _dbContext.SaveChangesAsync();

            return await BuildLoginResponse(user);
        }

        public async Task ChangePassword(int idEmployee, ChangePasswordDto dto)
        {
            var user = await _dbContext.Employees.FirstOrDefaultAsync(x => x.IdEmployee == idEmployee);
            if (user == null)
                throw new InvalidOperationException("User not found.");

            var hasPassword = !string.IsNullOrEmpty(user.PasswordHash);
            if (hasPassword)
            {
                if (string.IsNullOrEmpty(dto?.CurrentPassword))
                    throw new InvalidOperationException("Enter your current password.");

                var check = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash!, dto.CurrentPassword);
                if (check == PasswordVerificationResult.Failed)
                    throw new InvalidOperationException("Current password is incorrect.");
            }

            ValidateNewPassword(dto?.NewPassword, dto?.ConfirmPassword, user);

            if (hasPassword && dto!.NewPassword == dto.CurrentPassword)
                throw new InvalidOperationException("New password must be different from the current password.");

            SetPassword(user, dto!.NewPassword!);
            await _dbContext.SaveChangesAsync();
            _logger.LogInformation("Password changed for employee {IdEmployee}", idEmployee);
        }

        public async Task ResetPasswordWithOTP(ResetPasswordDto dto)
        {
            var email = dto?.EmailID?.Trim();
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(dto?.OTP))
                throw new InvalidOperationException("Enter Email ID and OTP.");

            var user = await _dbContext.Employees.FirstOrDefaultAsync(x => x.EmailID == email);
            if (user == null || user.CurrentStatus != "Working")
                throw new InvalidOperationException("Invalid Email ID or OTP.");

            var validFrom = DateTime.Now.AddMinutes(-ResetOtpValidMinutes);
            var loginOtp = await _dbContext.LoginOTP.FirstOrDefaultAsync(o =>
                o.EmailID == email && o.OTP == dto.OTP && o.OTPLoginStatus == "PENDING" && o.OTPSentDate >= validFrom);
            if (loginOtp == null)
                throw new InvalidOperationException("Invalid or expired OTP. Request a new OTP and try again.");

            ValidateNewPassword(dto.NewPassword, dto.ConfirmPassword, user);

            SetPassword(user, dto.NewPassword!);
            loginOtp.OTPLoginStatus = "USED";
            await _dbContext.SaveChangesAsync();
            _logger.LogInformation("Password reset with OTP for employee {IdEmployee}", user.IdEmployee);
        }

        public async Task<PasswordStatusDto> GetPasswordStatus(int idEmployee)
        {
            var user = await _dbContext.Employees.FirstOrDefaultAsync(x => x.IdEmployee == idEmployee);
            if (user == null)
                throw new InvalidOperationException("User not found.");

            return new PasswordStatusDto
            {
                HasPassword = !string.IsNullOrEmpty(user.PasswordHash),
                PasswordUpdatedOn = user.PasswordUpdatedOn
            };
        }

        private static void SetPassword(Employee user, string newPassword)
        {
            user.PasswordHash = _passwordHasher.HashPassword(user, newPassword);
            user.PasswordUpdatedOn = DateTime.Now;
            user.FailedLoginAttempts = 0;
            user.LockoutEndTime = null;
        }

        /// <summary>Min 8 characters with upper case, lower case, number and special character.</summary>
        private static void ValidateNewPassword(string? newPassword, string? confirmPassword, Employee user)
        {
            if (string.IsNullOrEmpty(newPassword))
                throw new InvalidOperationException("Enter a new password.");
            if (newPassword.Length < 8 || newPassword.Length > 50)
                throw new InvalidOperationException("Password must be 8 to 50 characters long.");
            if (newPassword.Any(char.IsWhiteSpace))
                throw new InvalidOperationException("Password must not contain spaces.");
            if (!newPassword.Any(char.IsUpper) || !newPassword.Any(char.IsLower) ||
                !newPassword.Any(char.IsDigit) || newPassword.All(char.IsLetterOrDigit))
                throw new InvalidOperationException(
                    "Password must contain an upper case letter, a lower case letter, a number and a special character.");
            if (newPassword != confirmPassword)
                throw new InvalidOperationException("New password and confirm password do not match.");

            var emailName = user.EmailID?.Split('@')[0];
            if (!string.IsNullOrWhiteSpace(emailName) && emailName.Length >= 4 &&
                newPassword.Contains(emailName, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Password must not contain your email name.");
        }

        private async Task<OTPStatusDto> BuildLoginResponse(Employee user)
        {
            var designation = await _dbContext.Designations
                .FirstOrDefaultAsync(x => x.IdDesignation == user.IdDesignation);

            var authClaims = new List<Claim>
            {
                new(ClaimTypes.Name, $"{user.FirstName} {user.LastName}"),
                new(ClaimTypes.Role, designation?.DesignationName ?? string.Empty),
                new(ClaimTypes.NameIdentifier, user.IdEmployee.ToString()),
                new(ClaimTypes.Email, user.EmailID ?? string.Empty),
                new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            // Same validity as OTP login (15 days)
            var authSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["JwtSettings:Secret"]));
            var token = new JwtSecurityToken(
                _configuration["JwtSettings:ValidIssuer"],
                _configuration["JwtSettings:ValidAudience"],
                expires: DateTime.Now.AddDays(15),
                claims: authClaims,
                signingCredentials: new SigningCredentials(authSigningKey, SecurityAlgorithms.HmacSha256));

            var dbPath = user.EmployeePhotoFilePath?.Trim();
            if (!string.IsNullOrEmpty(dbPath) && System.IO.File.Exists(dbPath))
                user.AttachmentBlob = await System.IO.File.ReadAllBytesAsync(dbPath);

            var authorizedModules = await _dbContext.EmployeePermissions
                .AnyAsync(ep => ep.IdEmployee == user.IdEmployee)
                ? "PAYROLL,SELFPORTAL"
                : "SELFPORTAL";

            return new OTPStatusDto
            {
                IdEmployee = user.IdEmployee ?? 0,
                OTPStatus = "SUCCESS",
                AuthorizedModules = authorizedModules,
                Name = $"{user.FirstName} {user.MiddleName} {user.LastName}".Trim(),
                Role = designation?.DesignationName ?? string.Empty,
                Token = new JwtSecurityTokenHandler().WriteToken(token),
                Email = user.EmailID,
                EmployeePhotoFilePath = user.EmployeePhotoFilePath,
                AttachmentBlob = user.AttachmentBlob
            };
        }
    }
}
