using System.ComponentModel.DataAnnotations.Schema;

namespace Georgetown_Internationsl_Academy.API.DTO
{
    public class MaternityLeaveSalaryDto
    {
        public int? IdMaternityLeaveSalary { get; set; }
        public int IdEmployee { get; set; }
        public DateTime MaternityLeaveFrom { get; set; }
        public DateTime MaternityLeaveTo { get; set; }
        public int IdSalaryMonthFrom { get; set; }
        public int IdSalaryMonthTo { get; set; }
        public decimal NetSalary { get; set; }
        public decimal? TotalEarnings { get; set; }
        public decimal? TotalDeductions { get; set; }        
        public decimal? MaternityLeaveNetSalary { get; set; }
        public decimal? DefaultNetSalary { get; set; }        
        public string? FromSalaryMonthText { get; set; }
        public string? ToSalaryMonthText { get; set; }
        public string? ApprovalStatus { get; set; }

        public IFormFile? File { get; set; }
        public string? DocumentFilePath { get; set; }

        [NotMapped]
        public byte[]? AttachmentBlob { get; set; }

        [NotMapped]
        public string? EmployeeCode { get; set; }
        [NotMapped]
        public string? EmployeeName { get; set; }
        [NotMapped]
        public int? IdDesignation { get; set; }
        [NotMapped]
        public string? DesignationName { get; set; }
        [NotMapped]
        public int? IdDepartment { get; set; }
        [NotMapped]
        public string? DepartmentName { get; set; }
        [NotMapped]
        public DateTime? JoiningDate { get; set; }
        [NotMapped]
        public string? Gender { get; set; }
        [NotMapped]
        public string? EmailID { get; set; }
        [NotMapped]
        public string? PhoneNumber1 { get; set; }
        [NotMapped]
        public string? PhoneNumber2 { get; set; }
        [NotMapped]
        public string? CurrentStatus { get; set; }
        public string? MaternityLeaveSalaryDetailDtoJson { get; set; }
        public List<MaternityLeaveSalaryDetailDto>? MaternityLeaveSalaryDetailDto { get; set; }

    }
}
