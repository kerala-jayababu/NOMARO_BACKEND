using Georgetown_Internationsl_Academy.API.DTO;

namespace Georgetown_Internationsl_Academy.API.Services.Interface
{
    public interface ISystemParameterService
    {
        Task<List<SystemParameterDto>> GetAllSystemParameters();
        Task<SystemParameterDto> GetSystemParameterById(int id);
        Task<SystemParameterDto> GetSystemParameterByName(string name);
        Task<bool> UpdateSystemParameter(SystemParameterDto dto);
    }
}
