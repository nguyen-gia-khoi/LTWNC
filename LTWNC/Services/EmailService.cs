using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Configuration;
using System.Threading.Tasks;

namespace LTWNC.Services
{
    // Interface được tách ra riêng
    public interface IEmailService
    {
        Task SendEmail(string receptor, string subject, string body);
    }

    // Class implement interface
    public class EmailService : IEmailService
    {
        private readonly IConfiguration configuration;

        public EmailService(IConfiguration configuration)
        {
            this.configuration = configuration;
        }

        public async Task SendEmail(string receptor, string subject, string body)
        {
            try
            {
                var email = configuration.GetValue<string>("EMAIL_CONFIGURATION:EMAIL");
                var password = configuration.GetValue<string>("EMAIL_CONFIGURATION:PASSWORD");
                var host = configuration.GetValue<string>("EMAIL_CONFIGURATION:HOST");
                var port = configuration.GetValue<int>("EMAIL_CONFIGURATION:PORT");

                // Kiểm tra null cho các giá trị quan trọng
                if (string.IsNullOrEmpty(email))
                {
                    throw new ArgumentNullException("EMAIL_CONFIGURATION:EMAIL", "Địa chỉ email người gửi không được cấu hình hoặc rỗng.");
                }
                if (string.IsNullOrEmpty(host))
                {
                    throw new ArgumentNullException("EMAIL_CONFIGURATION:HOST", "Host SMTP không được cấu hình hoặc rỗng.");
                }
                // Tương tự cho password nếu cần, nhưng password có thể rỗng tùy trường hợp

                var smtpClient = new SmtpClient(host, port);
                smtpClient.EnableSsl = true;
                smtpClient.UseDefaultCredentials = false;
                smtpClient.Credentials = new NetworkCredential(email, password);

                var message = new MailMessage(email, receptor, subject, body);
                await smtpClient.SendMailAsync(message);
            }
            catch (FormatException ex)
            {
                throw new ArgumentException("Địa chỉ email không hợp lệ. Vui lòng kiểm tra người gửi hoặc người nhận.", ex);
            }
            catch (Exception ex)
            {
                throw new Exception("Lỗi khi gửi email: " + ex.Message, ex);
            }
        }

    }
}
