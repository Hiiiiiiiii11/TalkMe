using ChatRepository.Models;
using ChatRepository.Models.Request;
using ChatService.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Share.Services;

namespace ChatAppAPI.Controllers.ChatAPI
{
    [ApiController]
    [Route("api/[controller]")]
    public class MessageController : ControllerBase
    {
        private readonly IMessageService _messageService;
        private readonly ICurrentUserService _currentUserService;
        public MessageController(IMessageService messageService ,ICurrentUserService currentUserService) {
            _messageService = messageService;
            _currentUserService = currentUserService;
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetMessage(Guid id)
        {
            try
            {
                if (!_currentUserService.Id.HasValue)
                {
                    return Unauthorized(new { message = "User not authenticated" });
                }
                var message = await _messageService.GetMessageByIdAsync(id);
                if (message == null)
                {
                    return NotFound(new { message = "Message not found" });
                }
                return Ok(message);
            }
            catch (UnauthorizedAccessException)
            {
                return Unauthorized(new { message = "User not authenticated" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            }
        [Authorize]
        [HttpGet("conversation/{conversationId}")]
        public async Task<IActionResult> GetMessageByRoom(Guid conversationId, [FromQuery] int? take, [FromQuery] DateTime? before)
        {
            try
            {
                if (take.HasValue && take <= 0)
                {
                    return BadRequest(new { message = "Take must be a positive integer." });
                }
                if (!_currentUserService.Id.HasValue)
                {
                    return Unauthorized(new { message = "User not authenticated" });
                }
                var currentUserId = _currentUserService.Id.Value;
                var messages = await _messageService.GetMessageByRoomIdAsync(conversationId, currentUserId, take, before);
                return Ok(messages);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [Authorize]
        [HttpPost("sendgroup")]
        public async Task<IActionResult> SendGroupMessage([FromBody] SendGroupMessageRequest request)
        {

            if (!_currentUserService.Id.HasValue)
            {
                return Unauthorized(new { message = "User not authenticated" });
            }
            try
            {
                var senderId = _currentUserService.Id.Value;
                var message = await _messageService.SendGroupMessageAsync(request, senderId);
                return Ok(message);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
        [Authorize]
        [HttpPost("sendprivate")]
        public async Task<IActionResult> SendPrivateMessage([FromBody] SendPrivateMessageRequest request)
        {
            if (!_currentUserService.Id.HasValue)
            {
                return Unauthorized(new { message = "User not authenticated" });
            }
            try
            {
                var senderId = _currentUserService.Id.Value;
                var message = await _messageService.SendPrivateMessageAsync(request, senderId);
                return Ok(message);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> EditMessage(Guid id, [FromBody] EditMessageRequest request)
        {
            try
            {
                if (!_currentUserService.Id.HasValue)
                {
                    return Unauthorized(new { message = "User not authenticated" });
                }
                await _messageService.EditMessageAsync(id, request);
                return Ok(new { message = "Edit message successfully" });
            }
            catch (UnauthorizedAccessException)
            {
                return Unauthorized(new { message = "User not authenticated" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });

            }
        }
        [HttpDelete("onlyuser/{id}")]
        public async Task<IActionResult> DeleteMessageOnlyUser(Guid id, [FromQuery] Guid userId)
        {
            try
            {
                if (!_currentUserService.Id.HasValue)
                {
                    return Unauthorized(new { message = "User not authenticated" });
                }
                await _messageService.DeleteMessageOnlyUserAsync(id, userId);
                return Ok(new { message = "Message deleted for current user" });
            }
            catch (UnauthorizedAccessException)
            {
                return Unauthorized(new { message = "User not authenticated" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
        [HttpDelete("all/{id}")]
        public async Task<IActionResult> DeleteMessageWithAll(Guid id)
        {
            try
            {
                if (!_currentUserService.Id.HasValue)
                {
                    return Unauthorized(new { message = "User not authenticated" });
                }
                await _messageService.DeleteMessageWithAllAsync(id);
                return Ok(new { message = "Message deleted for all users" });
            }
            catch (UnauthorizedAccessException)
            {
                return Unauthorized(new { message = "User not authenticated" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteMessage(Guid id)
        {
            try
            {
                if (!_currentUserService.Id.HasValue)
                {
                    return Unauthorized(new { message = "User not authenticated" });
                }
                await _messageService.DeleteMessageAsync(id);
                return Ok();
            }
            catch (UnauthorizedAccessException)
            {
                return Unauthorized(new { message = "User not authenticated" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}
