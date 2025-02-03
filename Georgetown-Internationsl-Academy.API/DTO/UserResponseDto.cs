using System.Data;

namespace Georgetown_Internationsl_Academy.API.DTO
{
    public class UserResponseDto
    {
        public string Name { get; set; }
        public int UserId { get; set; }
        public string Role { get; set; }
        public string Token { get; set; }
    }
}
