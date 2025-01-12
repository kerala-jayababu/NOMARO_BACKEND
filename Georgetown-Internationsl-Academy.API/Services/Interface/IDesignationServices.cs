using Georgetown_Internationsl_Academy.API.DTO;

namespace Georgetown_Internationsl_Academy.API.Services.Interface
{
    public interface IDesignationServices
    {
        Task<IEnumerable<DesignationDto>> GetDesignationList();
        Task<DesignationDto?> GetDesignationByID(int id);
        Task<DesignationDto?> AddDesignation(DesignationDto designation);
        Task<DesignationDto?> UpdateDesignation(DesignationDto designation);
    }

}
