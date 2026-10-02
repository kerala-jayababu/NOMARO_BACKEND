namespace Nomaro.API.DTO
{
    public class ChangePasswordDto
    {
        /// <summary>Required only when the employee already has a password.</summary>
        public string? CurrentPassword { get; set; }
        public string? NewPassword { get; set; }
        public string? ConfirmPassword { get; set; }
    }
}
