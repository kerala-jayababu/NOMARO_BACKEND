using Nomaro.API.DTO;

namespace Nomaro.API.Services.Interface
{
    public interface IEmployeeAgreementService
    {
        // HR
        Task<List<AgreementEmployeeOptionDto>> GetEmployees();
        Task<List<EmployeeAgreementListDto>> GetAgreements(EmployeeAgreementFilterDto filter);
        Task<EmployeeAgreementDetailDto?> GetAgreement(int idAgreement);
        Task<int> SendAgreement(SendEmployeeAgreementDto dto, int hrId, ClientInfo client);
        Task ResendAgreement(int idAgreement, int hrId, ClientInfo client);
        Task CancelAgreement(int idAgreement, string? reason, int hrId, ClientInfo client);
        Task<FileDownloadDto?> GetAgreementFile(int idAgreement);
        Task<FileDownloadDto?> GetCertificateFile(int idAgreement);

        // Public review page (token from the email link)
        Task<AgreementPortalViewDto> GetPortalView(string token, ClientInfo client);
        Task<FileDownloadDto?> GetPortalDocument(string token, ClientInfo client);
        Task<string> SendOtp(string token, ClientInfo client);
        Task<AgreementPortalViewDto> Respond(AgreementResponseDto dto, ClientInfo client);
    }
}
