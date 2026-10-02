namespace Nomaro.API.DTO
{
    public class ResetPasswordDto
    {
        public string? EmailID { get; set; }
        public string? OTP { get; set; }
        public string? NewPassword { get; set; }
        public string? ConfirmPassword { get; set; }
    }
}
