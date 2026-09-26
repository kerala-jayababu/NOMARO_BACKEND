using Nomaro.API.DTO;
using Microsoft.AspNetCore.Mvc;

namespace Nomaro.API.Services.Interface
{
    public interface IAccountService
    {
        Task<UserResponseDto> LoginForMail(int EmployeeID);
        Task<UserResponseDto> Login(LoginDto login);
        Task<UserResponseDto> DecryptToken(string token);
        Task<string> ValidateLogin(string emailId);

    }
}

