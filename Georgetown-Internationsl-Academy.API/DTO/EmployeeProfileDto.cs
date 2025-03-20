namespace Georgetown_Internationsl_Academy.API.DTO
{
    public class EmployeeProfileDto
    {
        public int IdEmployee { get; set; }
        public string EmployeeCode { get; set; }
        public string FullName { get; set; }
        public string Designation { get; set; }
        public string Department { get; set; }
        public DateTime JoiningDate { get; set; }
        public string CurrentStatus { get; set; }
        public int IdDepartment { get; set; }
        public int IdDesignation { get; set; }
        public string Gender { get; set; }
        public string EmailID { get; set; }
        public string PhoneNumber1 { get; set; }
        public string PhoneNumber2 { get; set; }
        public string? EmployeePhotoFilePath { get; set; }
        public byte[]? AttachmentBlob { get; set; }
    }
}
