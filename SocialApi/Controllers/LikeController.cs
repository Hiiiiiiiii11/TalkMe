using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Share.Services;
using SocialService.Services;

namespace SocialApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class LikeController : ControllerBase
    {
        private readonly ILikeService _likeService;
        private readonly ICurrentUserService _currentUserService;

        public LikeController(ILikeService likeService, ICurrentUserService currentUserService)
        {
            _likeService = likeService;
            _currentUserService = currentUserService;
        }
        [HttpPost("toggle/{postId}")]
        public async Task<IActionResult> ToggleLike(Guid postId)
        {
            // Lấy ID từ Token thông qua Service
            var userId = _currentUserService.Id;

            if (userId == null)
            {
                return Unauthorized(new { message = "Không tìm thấy thông tin người dùng." });
            }

            try
            {
                var result = await _likeService.ToggleLikeAsync(postId, userId.Value);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
        [HttpGet("hasLiked/{postId}")]
        public async Task<IActionResult> HasUserLikedPost(Guid postId)
        {
            var userId = _currentUserService.Id;

            if (userId == null)
            {
                return Unauthorized();
            }

            try
            {
                var hasLiked = await _likeService.HasUserLikedPostAsync(postId, userId.Value);
                return Ok(new { HasLiked = hasLiked });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}
