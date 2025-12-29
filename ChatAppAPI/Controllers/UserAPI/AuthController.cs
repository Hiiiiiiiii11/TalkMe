using Microsoft.AspNetCore.Mvc;
using System;
using UserService.Services;

using Microsoft.AspNetCore.Identity.Data;
using UserRepository.Model.Request;
using Share.Admin;



namespace ChatAppAPI.Controllers.UserAPI
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthenticationService _authService;
        private readonly AdminAccountSettings _adminAccountSettings;

        public AuthController(IAuthenticationService authService, AdminAccountSettings adminAccountSettings)
        {
            _authService = authService;
            _adminAccountSettings = adminAccountSettings;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginUserRequest request)
        {
            try
            {
                var response = await _authService.LoginAsync(request, _adminAccountSettings.Email);
                return Ok(response);
            }
            catch (InvalidOperationException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred in system.", error = ex.Message });
            }
        }
    }
}
