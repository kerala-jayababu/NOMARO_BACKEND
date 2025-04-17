namespace Georgetown_Internationsl_Academy.API.DTO
{
    public class OTPStatusDto
    {
        public int IdEmployee { get; set; }
        public string? OTPStatus { get; set; }
        public string? AuthorizedModules { get; set; }
        public string? Name { get; set; }

        public string? Role { get; set; }
        public string? Token { get; set; }
        public string? Email { get; set; }

        public string? EmployeePhotoFilePath { get; set; }
        public byte[]? AttachmentBlob { get; set; }
    }
}
