using Georgetown_Internationsl_Academy.API.DTO;
using Microsoft.AspNetCore.Mvc;

namespace Georgetown_Internationsl_Academy.API.Services.Interface
{
    public interface IAccountService
    {
        Task<UserResponseDto> LoginForMail(int EmployeeID);
        Task<UserResponseDto> Login(LoginDto login);
        Task<UserResponseDto> DecryptToken(string token);
        Task<string> ValidateLogin(string emailId);

    }
}
