using Microsoft.AspNetCore.Mvc;
using LTWNC.Services;  // Import namespace để dùng IEmailService và EmailService
using System.Threading.Tasks;

namespace LTWNC.Controllers
{
    [Route("api/emails")]
    [ApiController]
    public class EmailsController : ControllerBase
    {
        private readonly IEmailService emailService;

        public EmailsController(IEmailService emailService)
        {
            this.emailService = emailService;
        }

        [HttpPost]
        public async Task<IActionResult> SendEmail(string receptor, string subject, string body)
        {
            try
            {
                await emailService.SendEmail(receptor, subject, body);
                return Ok("Email sent successfully");
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);  // Trả về 400 với message lỗi
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Internal server error: " + ex.Message);  // Trả về 500 nếu lỗi khác
            }
        }
    }
}
