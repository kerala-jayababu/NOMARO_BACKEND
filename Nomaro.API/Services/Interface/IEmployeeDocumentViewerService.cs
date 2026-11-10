using Nomaro.API.DTO;

namespace Nomaro.API.Services.Interface
{
    public interface IEmployeeDocumentViewerService
    {
        Task<List<EmployeeDocumentListDto>> GetDocuments(EmployeeDocumentFilterDto filter);
        Task<FileDownloadDto?> GetDocumentFile(int idEmployeeDocument);
    }
}
