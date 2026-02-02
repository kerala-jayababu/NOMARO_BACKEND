using System.ComponentModel.DataAnnotations;

namespace Georgetown_Internationsl_Academy.API.DTO
{
    public class EmployeeDto
    {
        [Key]
        public int? IdEmployee { get; set; }
        public string? EmployeeCode { get; set; }
        public string FirstName { get; set; }
        public string? MiddleName { get; set; }
        public string? LastName { get; set; }
        public string? EmployeeWorkType { get; set; }

        public string? Gender { get; set; }
        public string? IdNumber { get; set; }
        public string? TaxIdNumber { get; set; }
        public string? NationalIDNumber { get; set; }
        public int? IdDepartment { get; set; }
        public int? IdDesignation { get; set; }
        public string? EmailID { get; set; }
        public string? PhoneNumber1 { get; set; }
        public string? PhoneNumber2 { get; set; }
        public string? WhatsAppNumber { get; set; }
        public string? Address1 { get; set; }
        public string? Address2 { get; set; }
        public string? Address3 { get; set; }
        public string? City { get; set; }
        public string? State { get; set; }
        public string? ZipCode { get; set; }
        public DateTime? JoiningDate { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public int? ReportingTo { get; set; }
        public string? CurrentStatus { get; set; }
        public DateTime? LastWorkingDay { get; set; }
        public int? IdBudgetCode { get; set; }
        public int? ChildrenCount { get; set; }
        public string? OverTimeAllowedStatus { get; set; }

    }

}
