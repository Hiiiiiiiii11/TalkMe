using Microsoft.AspNetCore.Mvc;
using NotificationRepository.Model.Request;
using NotificationRepository.Model.Response;
using NotificationRepository.Models;
using NotificationService.Services;
using Share.Models.Request;
using Share.Services;

namespace NotificationApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class NotificationController : ControllerBase
    {
        private readonly INotificationService _notificationService;
        private readonly ICurrentUserService _currentUserService;

        public NotificationController(INotificationService notificationService)
        {
            _notificationService = notificationService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAllNotifications()
        {
            try
            {
                var notifications = await _notificationService.GetAllNotificationsAsync();
                return Ok(notifications);
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
        [HttpGet("{id}")]
        public async Task<IActionResult> GetNotificationById(Guid id)
        {
            try
            {
                var notification = await _notificationService.GetNotificationByIdAsync(id);
                if (notification == null)
                {
                    return NotFound(new { message = "Notification not found" });
                }
                return Ok(notification);
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
        [HttpGet("user/{userId}")]
        public async Task<IActionResult> GetNotificationsByUserId(Guid userId, [FromQuery] PagingRequest request)
        {
            try
            {
                var notifications = await _notificationService.GetNotificationsByUserIdAsync(userId, request);
                return Ok(notifications);
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
        [HttpGet("user/message/{userId}")]
        public async Task<IActionResult> GetNotificationsMessageByUserId(Guid userId)
        {
            try {
                var notifications = await _notificationService.GetNotificationsMessageByUserIdAsync(userId);
                return Ok(notifications);
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
        [HttpGet("user/system/{userId}")]
        public async Task<IActionResult> GetNotificationsSystemByUserId(Guid userId, [FromQuery] PagingRequest request)
        {
            try
            {
                var notifications = await _notificationService.GetNotificationsSystemByUserIdAsync(userId,request);
                return Ok(notifications);

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
        // Tạo thông báo khi có tin nhắn mới
        [HttpPost("message")]
        public async Task<ActionResult<NotificationMessageResponse>> CreateMessageNotification([FromBody] CreateMessageNotificationRequest request)
        {
            try
            {
                var createdNotification = await _notificationService.CreateMessageNotificationAsync(request);

                return CreatedAtAction(
                    nameof(GetNotificationById),
                    new { id = createdNotification.Id },
                    createdNotification
                );
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            } 
        }
        //tạo thông báo cho user
        [HttpPost("user")]
        public async Task<ActionResult<NotificationMessageResponse>> CreateNotificationForUser([FromBody] CreateUserNotificationRequest request)
        {
            try
            {
                var createdNotification = await _notificationService.CreateNotificationForUserAsync(request);

                return CreatedAtAction(
                    nameof(GetNotificationById),
                    new { id = createdNotification.Id },
                    createdNotification
                );
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            } 
        }
        [HttpPost("markasread/{id}")]
        public async Task<IActionResult> MarkAsRead(Guid id)
        {
            try
            {
                if (id == Guid.Empty)
                {
                    return BadRequest(new { message = "Invalid ID" });
                }
                var notification = await _notificationService.GetNotificationByIdAsync(id);
                if (notification == null)
                {
                    return NotFound(new { message = "Notification not found" });
                }
                await _notificationService.MarkAsReadAsync(id);
                return NoContent();
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateNotification(Guid id, [FromBody] Notification notification)
        {
            try
            {
                if (id != notification.Id)
                {
                    return BadRequest(new { message = "ID mismatch" });
                }
                var existingNotification = await _notificationService.GetNotificationByIdAsync(id);
                if (existingNotification == null)
                {
                    return NotFound(new { message = "Notification not found" });
                }
                await _notificationService.UpdateNotificationAsync(notification);
                return NoContent();
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            } 
        }
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteNotification(Guid id)
        {
            try
            {
                if (id == Guid.Empty)
                {
                    return BadRequest(new { message = "Invalid ID" });
                }
                var notification = await _notificationService.GetNotificationByIdAsync(id);
                if (notification == null)
                {
                    return NotFound(new { message = "Notification not found" });
                }
                await _notificationService.DeleteNotificationAsync(id);
                return NoContent();
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }


    }
}
