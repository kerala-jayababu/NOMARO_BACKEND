namespace Georgetown_Internationsl_Academy.API.DTO
{
    public class EmployeeEntityDto
    {      
        public string? EmployeeCode { get; set; }
        public string? FirstName { get; set; }
        public string? MiddleName { get; set; }
        public string? LastName { get; set; }
        public string? Gender { get; set; }
        public string? EmployeeWorkType { get; set; }
        public string? IdNumber { get; set; }
        public string? TaxIdNumber { get; set; }
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
        public DateTime? DateOfBirth { get; set; }
        public DateTime? JoiningDate { get; set; }
        public int? ReportingTo { get; set; }
        public string? CurrentStatus { get; set; }
        public DateTime? LastWorkingDay { get; set; }
        public int? IdBudgetCode { get; set; }      
        public string? EmployeePhotoFilePath { get; set; }
        public string? ChildCountDocumentFilePath { get; set; }
        public string? OverTimeAllowedStatus { get; set; }

        // File Upload
        public IFormFile? EmployeePhoto { get; set; }
    }
}
