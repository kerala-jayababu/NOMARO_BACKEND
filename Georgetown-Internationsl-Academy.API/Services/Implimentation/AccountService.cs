using Georgetown_International_Academy.API.Database;
using Georgetown_Internationsl_Academy.API.DTO;
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

        public AccountService(IConfiguration configuration, ApplicationDBContext dbContext)
        {
            _configuration = configuration;
            _dbContext = dbContext;
        }

        public async Task<UserResponseDto> Login(LoginDto login)
        {
            var user = _dbContext.Employees.FirstOrDefault(x => x.EmailID == login.Email);
            if (user == null)
            {
                return null;
            }

            if (login.Password != "Welcome@1")
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
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            var token = GetToken(authClaims);


            var userResponse = new UserResponseDto
            {
                Name = user.FirstName + " " + user.LastName,
                UserId = user.IdEmployee,
                Role = designation != null ? designation.DesignationName : string.Empty,
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
    }
}
