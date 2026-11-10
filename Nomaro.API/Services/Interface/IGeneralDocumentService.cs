using Nomaro.API.DTO;

namespace Nomaro.API.Services.Interface
{
    public interface IGeneralDocumentService
    {
        /// <summary>Permission strings (e.g. "VAUD") of the employee for the given screen codes, same source as CheckEmployeePermission.</summary>
        Task<Dictionary<string, string>> GetScreenPermissions(int idEmployee, params string[] screenCodes);

        // Masters
        Task<List<GeneralDocumentTypeDto>> GetTypes(bool includeInactive);
        Task<int> SaveType(GeneralDocumentTypeDto dto, int idEmployee);
        Task<List<GeneralDocumentSubtypeDto>> GetSubtypes(int? idType, bool includeInactive);
        Task<int> SaveSubtype(GeneralDocumentSubtypeDto dto, int idEmployee);

        // Documents
        Task<GeneralDocumentLookupsDto> GetLookups(GeneralDocumentUserContext user);
        Task<List<GeneralDocumentListDto>> GetDocuments(GeneralDocumentFilterDto filter, GeneralDocumentUserContext user);
        Task<GeneralDocumentSummaryDto> GetSummary(GeneralDocumentFilterDto filter, GeneralDocumentUserContext user);
        Task<GeneralDocumentDetailDto> GetDocument(int idGeneralDocument, GeneralDocumentUserContext user);
        Task<int> SaveDocument(SaveGeneralDocumentDto dto, GeneralDocumentUserContext user);
        Task SetDocumentActive(int idGeneralDocument, bool isActive, string? remarks, GeneralDocumentUserContext user);

        // Records and files
        Task<int> UploadRecord(UploadGeneralDocumentRecordDto dto, GeneralDocumentUserContext user);
        Task UpdateRecord(UpdateGeneralDocumentRecordDto dto, GeneralDocumentUserContext user);
        Task SetRecordStatus(GeneralDocumentRecordStatusDto dto, GeneralDocumentUserContext user);
        Task<FileDownloadDto> GetFile(int idGeneralDocumentFile, bool inline, GeneralDocumentUserContext user);
        Task<FileDownloadDto> GetZip(GeneralDocumentZipRequestDto request, GeneralDocumentUserContext user);

        // Acknowledgements (Self Portal)
        Task<List<MyPublicDocumentDto>> GetMyDocuments(int idEmployee);
        Task<FileDownloadDto> GetMyDocumentFile(int idGeneralDocumentFile, int idEmployee, string? ip, string? userAgent);
        Task<DateTime> AcknowledgeMyDocument(int idGeneralDocumentRecord, int idEmployee, string? ip, string? userAgent);

        // Acknowledgement dashboard
        Task<AckDashboardDto> GetAckDashboard(int? idGeneralDocumentType, GeneralDocumentUserContext user);
        Task<List<AckEmployeeDto>> GetAckEmployees(int idGeneralDocument, GeneralDocumentUserContext user);
        Task<FileDownloadDto> ExportAckEmployees(int idGeneralDocument, string? status, GeneralDocumentUserContext user);
        Task<AckReminderResultDto> SendAckReminders(AckReminderRequestDto request, GeneralDocumentUserContext user);

        // Reports
        Task<GeneralDocumentReportDto> GetReport(GeneralDocumentReportFilterDto filter, GeneralDocumentUserContext user);
        Task<FileDownloadDto> ExportReport(GeneralDocumentReportFilterDto filter, GeneralDocumentUserContext user);
    }
}
