namespace Georgetown_Internationsl_Academy.API.DTO
{
    public class EmployeeProfileDetailsDto
    {
        public int IdEmployee {  get; set; }
        public string EmployeeCode { get; set; }
        public string FullName { get; set; }
        public string EmailId { get; set; }
        public string? SSNNumber { get; set; }
        public string PhoneNumber1 { get; set; }
        public string PhoneNumber2 { get; set; }
        public string WhatsAppNumber { get; set; }
        public string Department { get; set; }
        public string Designation { get; set; }
        public string ReportingTo { get; set; }
        public string BudgetCode { get; set; }
        public string TaxIdNumber { get; set; }
        public string Address { get; set; }
        public string CurrentStatus { get; set; }
        public DateTime? DateOfBirth {  get; set; }
        public DateTime JoiningDate { get; set; }       
        public string Gender { get; set; }
        public string? EmployeePhotoFilePath { get; set; }
        public byte[]? AttachmentBlob { get; set; }
        public string? EmployeeWorkType { get; set; }
        public string? OverTimeAllowedStatus { get; set; }
        public List<EmployeeBankAccountDto> BankAccounts { get; set; }
    }
}
