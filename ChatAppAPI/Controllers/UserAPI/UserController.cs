using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;
using UserService.Services;
using Microsoft.AspNetCore.Authorization;
using UserService.Model.Response;
using Microsoft.AspNetCore.Http.HttpResults;
using UserRepository.Model.Request;
using UserRepository.Models;
using Share.Services;
using Share.Models.Request;

namespace ChatAppAPI.Controllers.UserAPI
{
    [ApiController]
    [Route("api/[controller]")]
    public class UserController : ControllerBase
    {
        private readonly IUserService _userService;
        private readonly IUploadPhotoService _photoService;
        private readonly IEmailVerificationService _emailVerificationService;
        private readonly IPasswordResetService _passwordResetService;
        private readonly ICurrentUserService _currentUserService;

        public UserController(IUserService userService, IUploadPhotoService photoService, IEmailVerificationService emailVerificationService, IPasswordResetService passwordResetService,ICurrentUserService currentUserService)
        {
            _userService = userService;
            _photoService = photoService;
            _emailVerificationService = emailVerificationService;
            _passwordResetService = passwordResetService;
            _currentUserService = currentUserService;
        }

        [HttpPost]
        public async Task<IActionResult> CreateUser([FromBody] RegisterUserRequest request)
        {
            try
            {
                var createdUser = await _userService.AddUserAsync(request);
                return  CreatedAtAction(nameof(GetUserById), new { id = createdUser.Id }, createdUser);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
        [Authorize]
        [HttpGet("{id}")]
        public async Task<IActionResult> GetUserById(Guid id )
        {
            var user = await _userService.GetUserByIdAsync(id);
            if (user == null)
                return NotFound(new { message = "User not found." });

            return Ok(user);
        }

        [HttpGet]
        public async Task<IActionResult> GetAllUsers()
        {
            try
            {
                var users = await _userService.GetAllUsersAsync();
                return Ok(users);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
        [Authorize]
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateUser(Guid id, [FromForm] UpdateUserRequest request)
        {
            try
            {
                if (!_currentUserService.Id.HasValue)
                {
                    return Unauthorized(new { message = "User not authenticated" });
                }
                if (_currentUserService.Id.Value != id)
                {
                    return Unauthorized(new { message = "User not authenticated" }); 
                }
                var updatedUser = await _userService.UpdateUserAsync(id, request);
                return Ok(new
                {
                    message = "User info updated successfully",
                    data = updatedUser
                });
            }
            catch (KeyNotFoundException)
            {
                return NotFound(new { message = "User not found" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }

        }

        [Authorize(Roles = "Admin")]
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteUser(Guid id)
        {
            try
            {
                var user = await _userService.GetUserByIdAsync(id);
                await _userService.DeleteUserAsync(id);
                return Ok(new { message = "User deleted successfully." });
            }
            catch (KeyNotFoundException)
            {
                return NotFound(new { message = "User not found." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("search")]
        public async Task<IActionResult> SearchUsers([FromQuery]string displayName, [FromQuery] PagingRequest request)
        {
            try
            {
                if (string.IsNullOrEmpty(displayName))
                    return BadRequest(new { message = "Search term is required." });
                var users = await _userService.SearchUsersAsync(displayName, request);
                return Ok(users);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
               
        }
        [Authorize(Roles = "Admin")]
        [HttpPut("unactive/{id}")]
        public async Task<IActionResult> UnActiveUser(Guid id)
        {
            try
            {
                await _userService.UnActiveUser(id);
                return Ok(new { message = "User deactivated successfully." });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = "User isn't exist!" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = "No data update provide." });
            }
        }
        [Authorize(Roles = "Admin")]
        [HttpPut("active/{id}")]
        public async Task<IActionResult> ActiveUser(Guid id)
        {
            try
            {
                await _userService.ActiveUser(id);
                return Ok(new { message = "User activated successfully." });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = "User isn't exist!" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = "No data update provide." });
            }
        }
    }
}
