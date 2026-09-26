using Nomaro.API.DTO;

namespace Nomaro.API.Services.Interface
{
    public interface IDesignationServices
    {
        Task<IEnumerable<DesignationDto>> GetDesignationList();
        Task<DesignationDto?> GetDesignationByID(int id);
        Task<DesignationDto?> AddDesignation(DesignationDto designation);
        Task<DesignationDto?> UpdateDesignation(DesignationDto designation);
    }

}

