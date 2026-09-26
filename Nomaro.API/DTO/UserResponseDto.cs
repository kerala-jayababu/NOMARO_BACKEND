using System.Data;

namespace Nomaro.API.DTO
{
    public class UserResponseDto
    {
        public string Name { get; set; }
        public int UserId { get; set; }
        public string Role { get; set; }
        public string Token { get; set; }
        public string? AuthorizedModules { get; set; }
        public string? Email { get; set; }
        public int ? IdEmployee { get; set; }

        public string? EmployeePhotoFilePath {  get; set; }
        public byte[]? AttachmentBlob { get; set; }
    }
}

