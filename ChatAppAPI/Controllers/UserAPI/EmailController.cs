using Microsoft.AspNetCore.Mvc;
using UserService.Services;
using System.Threading.Tasks;
using UserRepository.Model.Request;

namespace ChatAppAPI.Controllers.UserAPI
{
    [ApiController]
    [Route("api/[controller]")]
    public class EmailController : ControllerBase
    {
        private readonly IEmailVerificationService _emailVerificationService;

        public EmailController(IEmailVerificationService emailVerificationService)
        {
            _emailVerificationService = emailVerificationService;
        }

        [HttpPost("send-otp")]
        public async Task<IActionResult> SendOtp([FromQuery] string email)
        {
            try
            {
                if (string.IsNullOrEmpty(email))
                {
                    return BadRequest(new { message = "Email is required." });
                }
                await _emailVerificationService.SendVerificationCodeAsync(email);
                return Ok(new { message = "OTP sent to your email." });
            }
            catch
            {
                return BadRequest(new { message = "Invalid email format." });
            }
        }

        [HttpPost("verify-otp")]
        public async Task<IActionResult> VerifyOtp([FromBody] VerifyOTPRequest request)
        {
            try
            {
                if (string.IsNullOrEmpty(request.Email) || string.IsNullOrEmpty(request.Code))
                {
                    return BadRequest(new { message = "Email and code are required." });
                }
                var result = await _emailVerificationService.VerifyCodeAsync(request);
                if (!result) return BadRequest(new { message = "Invalid or expired code." });
                return Ok(new { message = "Email verified successfully." });
            }
            catch
            {
                return BadRequest(new { message = "Invalid request." });
            }
        }
    }
}
