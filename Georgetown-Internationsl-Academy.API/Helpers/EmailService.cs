using System.Net.Mail;
using System.Net;

namespace Georgetown_Internationsl_Academy.API.Helpers
{
    public static class EmailService
    {
        public static async Task<bool> SendMail(string toEmail, string subject, string htmlBody)
        {
            try
            {
                using (var client = new SmtpClient("smtp.gmail.com", 587))
                {
                    client.EnableSsl = true;
                    client.Credentials = new NetworkCredential("georgetowninternational123@gmail.com", "osctzjyjppggggas");

                    var mailMessage = new MailMessage
                    {
                        From = new MailAddress("georgetowninternational123@gmail.com", "George Town International Academy"),
                        Subject = subject,
                        Body = htmlBody, 
                        IsBodyHtml = true 
                    };

                    mailMessage.To.Add(toEmail);
                    await client.SendMailAsync(mailMessage);
                }
                return true;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
    }
}
