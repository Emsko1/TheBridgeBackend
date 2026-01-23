using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Configuration;
using System.Threading.Tasks;

namespace Bridge.Backend.Services
{
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _configuration;

        public EmailService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task SendEmailAsync(string toEmail, string subject, string message)
        {
            var smtpHost = _configuration["EmailSettings:SmtpHost"];
            var smtpPort = int.Parse(_configuration["EmailSettings:SmtpPort"] ?? "587");
            var smtpUser = _configuration["EmailSettings:SmtpUser"];
            var smtpPass = _configuration["EmailSettings:SmtpPass"];

            if (string.IsNullOrEmpty(smtpHost) || string.IsNullOrEmpty(smtpUser) || string.IsNullOrEmpty(smtpPass))
            {
                // Log warning or throw exception depending on requirement. 
                // For now, we'll just return if config is missing to avoid crashing if not set up.
                System.Console.WriteLine($"[EmailService] Email sending skipped. Configuration missing. To: {toEmail}, Subject: {subject}, Body: {message}");
                return;
            }

            var client = new SmtpClient(smtpHost, smtpPort)
            {
                Credentials = new NetworkCredential(smtpUser, smtpPass),
                EnableSsl = true
            };

            var mailMessage = new MailMessage
            {
                From = new MailAddress(smtpUser),
                Subject = subject,
                Body = message,
                IsBodyHtml = true,
            };
            mailMessage.To.Add(toEmail);

            await client.SendMailAsync(mailMessage);
        }
    }
}
