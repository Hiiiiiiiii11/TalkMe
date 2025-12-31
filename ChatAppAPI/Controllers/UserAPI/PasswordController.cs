using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UserRepository.Model.Request;
using UserService.Services;

namespace ChatAppAPI.Controllers.UserAPI
{
    [ApiController]
    [Route("api/[controller]")]
    public class PasswordResetController : ControllerBase
    {
        private readonly IPasswordResetService _resetService;

        public PasswordResetController(IPasswordResetService resetService)
        {
            _resetService = resetService;
        }

        [HttpPost("request")]
        public async Task<IActionResult> RequestReset([FromQuery] PasswordResetRequest request)
        {
            try
            {
                if (string.IsNullOrEmpty(request.Email))
                {
                    return BadRequest(new { message = "Email is required." });
                }
                await _resetService.RequestPasswordResetAsync(request);
                return Ok(new { message = "OTP sent to your email." });
            }
            catch
            {
                return BadRequest(new { message = "Invalid email format." });
            }
        }
        [HttpPost("confirm")]
        public async Task<IActionResult> ConfirmReset([FromBody] ConfirmResetPasswordRequest request)
        {
            try
            {
                if (string.IsNullOrEmpty(request.Email) || string.IsNullOrEmpty(request.Otp) || string.IsNullOrEmpty(request.NewPassword))
                {
                    return BadRequest(new { message = "Email, code and new password are required." });
                }
                request.Email = request.Email.Trim();
                request.Otp = request.Otp.Trim();
                await _resetService.ResetPasswordAsync(request);
                return Ok(new { message = "Password updated successfully." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }

        }
    }
}
