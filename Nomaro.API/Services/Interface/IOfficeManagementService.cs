using Nomaro.API.DTO;

namespace Nomaro.API.Services.Interface
{
    public interface IOfficeManagementService
    {
        // Office Types
        Task<IEnumerable<OfficeTypeDto>> GetOfficeTypes(bool? isActive);
        Task<bool> IsOfficeTypeCodeExists(string officeTypeCode, int idOfficeType);
        Task<bool> AddOrUpdateOfficeTypes(OfficeTypeDto dto, int idLoggedInEmployee);

        // Offices
        Task<IEnumerable<OfficeDto>> GetOffices(string? searchText, int? idOfficeType, int? idParentOffice, bool? isActive);
        Task<OfficeDto?> GetOfficeById(int idOffice);
        Task<bool> IsOfficeCodeExists(string officeCode, int idOffice);
        Task<int> AddOffice(OfficeDto dto, int idLoggedInEmployee);
        Task<bool> UpdateOffice(OfficeDto dto, int idLoggedInEmployee);
        Task<bool> UpdateOfficeStatus(int idOffice, bool isActive, int idLoggedInEmployee);

        // Employee Office Postings
        Task<IEnumerable<OfficeEmployeeDto>> GetOfficeEmployees(int idOffice, bool isCurrentOnly);
        Task<IEnumerable<EmployeeOfficePostingDto>> GetEmployeeOfficePostings(int idEmployee);
        Task<bool> IsEmployeeOfficePostingExists(EmployeeOfficePostingDto dto);
        Task<int> AddOrUpdateEmployeeOfficePosting(EmployeeOfficePostingDto dto, int idLoggedInEmployee);
    }
}
