using LTWNC.API;
using LTWNC.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace LTWNC.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AccountController : ControllerBase
    {
        private readonly JwtServices _jwtServices;
        public AccountController(JwtServices jwtServices)
        {
            _jwtServices = jwtServices;
        }

        [AllowAnonymous]
        [HttpPost("login")]
        public async Task<ActionResult<LoginResponseModle>> Login(LoginRequestModel request)
        {
            
            var result = await _jwtServices.Authenticate(request);
            Console.WriteLine($"[DEBUG] Authentication result: {(result != null ? "Success" : "Failed")}");

            if (result == null)
            {
                return Unauthorized(new { message = "Invalid email or password." });
            }
            return Ok(result);
        }

        [Authorize]
        [HttpGet("user-info")]
        public async Task<ActionResult<object>> GetUserInfo()
        {
            var email = User.Identity?.Name;
            if (string.IsNullOrEmpty(email))
            {
                return Unauthorized();
            }

            var role = User.Claims.FirstOrDefault(c => c.Type == "role")?.Value;

            return Ok(new
            {
                email,
                role
            });
        }
    }
}
