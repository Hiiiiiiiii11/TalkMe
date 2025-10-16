
using ChatRepository.Model.Request;
using ChatRepository.Models;
using ChatService.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualBasic;
using Share.Services;


namespace ChatAppAPI.Controllers.ChatAPI
{
    [ApiController]
    [Route("api/[controller]")]
    public class ConversationController : ControllerBase
    {
        private readonly IConversationService _conversationService;
        private readonly ICurrentUserService _currentUserService;

        public ConversationController(IConversationService conversationService, ICurrentUserService currentUserService)
        {
            _conversationService = conversationService;
            _currentUserService = currentUserService;
        }
        [HttpGet("{id}")]
        public async Task<IActionResult> GetConversation(Guid id)
        {
            try
            {
                if (!_currentUserService.Id.HasValue)
                {
                    return Unauthorized(new { message = "User not authenticated" });
                }
                var conversation = await _conversationService.GetConversationByIdAsync(id);
                if (conversation == null)
                {
                    return NotFound(new { message = "Conversation not found" });
                }
                return Ok(conversation);
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
        [HttpGet("user/{userId}")]
        public async Task<IActionResult> GetUserConversations(Guid userId)
        {
            try
            {
                if (!_currentUserService.Id.HasValue || _currentUserService.Id.Value != userId)
                {
                    return Unauthorized(new { message = "User not authenticated" });
                }
                var conversations = await _conversationService.GetUserConversationsAsync(userId);
                return Ok(conversations);
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
        [HttpPost]
        public async Task<IActionResult> CreateConversation([FromQuery] ConversationCreateRequest request)
        {
            try
            {
                if (!_currentUserService.Id.HasValue)
                {
                    return Unauthorized(new { message = "User not authenticated" });
                }
                var creatorId = _currentUserService.Id.Value;
                var conversation = await _conversationService.CreateConversationAsync(request, creatorId);
                return CreatedAtAction(nameof(GetConversation), new { id = conversation.Id }, conversation);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }

        }
        [Authorize]
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateConversation(Guid id, [FromForm] ConversationUpdateRequest request)
        {
            try
            {
                if (request == null)
                {
                    return BadRequest(new { message = "Invalid request data" });
                }
                if (!_currentUserService.Id.HasValue)
                {
                    return Unauthorized(new { message = "User not authenticated" });
                }
                var adminId = _currentUserService.Id.Value;

                var conversation = await _conversationService.GetConversationByIdAsync(id);
                if (conversation == null)
                {
                    return NotFound(new { message = "Conservation isn't exist" });
                }
                // chỉ admin hiện tại mới có quyền update
                if (conversation.AdminId != adminId)
                {
                    return Unauthorized(new { message = "Only admin can update info on group " });
                }
                await _conversationService.UpdateConversationAsync(id, request, adminId);

                return Ok(new { message = "Conversation updated successfully" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
        [Authorize]
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteGroupConversation(Guid id)
        {
            try
            {
                if (!_currentUserService.Id.HasValue)
                {
                    return Unauthorized(new { message = "User not authenticated" });
                }
                var currentUserId = _currentUserService.Id.Value;
                var conversation = await _conversationService.GetConversationByIdAsync(id);
                if (conversation == null)
                {
                    return NotFound(new { message = "Conversation not found" });
                }
                // Nếu là group thì chỉ admin mới có quyền xóa
                if (conversation.IsGroup && conversation.AdminId != currentUserId)
                {
                    return Unauthorized(new { message = "Only admin can delete this group conversation" });
                }
                await _conversationService.DeleteConversationAsync(id);
                return Ok(new { message = "Delete Conversation success" });

            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
        [Authorize]
        [HttpPut("dissolve/{id}")]
        public async Task<IActionResult> DissolveGroupConversation(Guid id)
        {
            try
            {
                if (!_currentUserService.Id.HasValue)
                {
                    return Unauthorized(new { message = "User not authenticated" });
                }
                var currentUserId = _currentUserService.Id.Value;
                var conversation = await _conversationService.GetConversationByIdAsync(id);
                if (conversation == null)
                {
                    return NotFound(new { message = "Conversation not found" });
                }

                // Nếu là group thì chỉ admin mới có quyền xóa
                if (conversation.IsGroup && conversation.AdminId != currentUserId)
                {
                    return Unauthorized(new { message = "Only admin can dissolve this group conversation" });
                }
                await _conversationService.DissolveConversationAsync(id);

                return Ok(new { message = "Dissolve Conversation success" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}
