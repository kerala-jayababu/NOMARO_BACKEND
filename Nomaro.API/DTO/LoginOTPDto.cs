namespace Nomaro.API.DTO
{
    public class LoginOTPDto
    {
         public int  IdLoginOTP { get; set; }
        public int IdEmployee { get; set; }
        public string? EmailID { get; set; }
        public string? OTP { get; set; }
        public DateTime OTPSentDate { get; set; }
        public  string? OTPLoginStatus { get; set; }
    }
}

