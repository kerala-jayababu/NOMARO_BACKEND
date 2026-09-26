using Nomaro.API.DTO;

namespace Nomaro.API.Services.Interface
{
    public interface ISystemParameterService
    {
        Task<List<SystemParameterDto>> GetAllSystemParameters();
        Task<SystemParameterDto> GetSystemParameterById(int id);
        Task<SystemParameterDto> GetSystemParameterByName(string name);
        Task<bool> UpdateSystemParameter(SystemParameterDto dto);
    }
}

