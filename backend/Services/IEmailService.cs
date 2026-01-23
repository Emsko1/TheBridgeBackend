using System.Threading.Tasks;

namespace Bridge.Backend.Services
{
    public interface IEmailService
    {
        Task SendEmailAsync(string toEmail, string subject, string message);
    }
}
