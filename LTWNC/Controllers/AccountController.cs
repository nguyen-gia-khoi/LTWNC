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
            Console.WriteLine($"[DEBUG] Received email: {request.Email}, password: {request.Password}");
            var result = await _jwtServices.Authenticate(request);

            if (result == null)
            {
                return Unauthorized(new { message = "Invalid email or password." });
            }
            return Ok(result);
        }
    }
}
