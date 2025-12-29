using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Share.Services;
using SocialRepository.Model.Request;
using SocialService.Services;

namespace SocialApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class PostController : ControllerBase
    {
        private readonly IPostService _postService;
        private readonly ICurrentUserService _currentUserService;
        public PostController(IPostService postService, ICurrentUserService currentUserService )
        {
            _postService = postService;
            _currentUserService = currentUserService;

        }
        [HttpGet("public")]
        public async Task<IActionResult> GetPublicPosts([FromQuery] int take = 10, [FromQuery] DateTime? before = null)
        {
            try
            {
                var posts = await _postService.GetPublicPostAsync(take, before);
                return Ok(posts);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
        [HttpGet("user/{userId}")]
        public async Task<IActionResult> GetUserPosts([FromRoute] Guid userId, [FromQuery] int take = 10, [FromQuery] DateTime? before = null)
        {
            try
            {
                var posts = await _postService.GetUserPostAsync(userId, take, before);
                return Ok(posts);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
        [HttpGet("{postId}")]
        public async Task<IActionResult> GetPostById([FromRoute] Guid postId)
        {
            try
            {
                var post = await _postService.GetPostByIdAsync(postId);
                return Ok(post);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> CreatePost([FromForm] PostRequest request)
        {
            // Lấy ID người dùng hiện tại từ Token
            var currentUserId = _currentUserService.Id;

            if (currentUserId == null)
            {
                return Unauthorized(new { message = "User not authenticated" });
            }

            try
            {
                //lấy user Id từ token đang đăng nhập
                var post = await _postService.CreatePostAsync(currentUserId.Value,request);
                return Ok(post);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
        [HttpPut("{postId}")]
        // SỬA: Dùng [FromForm] vì PostUpdateRequest có chứa file upload
        public async Task<IActionResult> UpdatePost([FromRoute] Guid postId, [FromForm] PostUpdateRequest request)
        {
            var currentUserId = _currentUserService.Id;

            if (currentUserId == null)
            {
                return Unauthorized(new { message = "User not authenticated" });
            }
            try
            {
                var post = await _postService.UpdatePostAsync(postId, request);
                if (post == null) return NotFound();
                if (post.UserId != currentUserId.Value)
                {
                    return Unauthorized(new {message = "Only creator can update"});
                }

                return Ok(post);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
        [HttpDelete("{postId}")]
        public async Task<IActionResult> DeletePost([FromRoute] Guid postId)
        {
            var currentUserId = _currentUserService.Id;

            if (currentUserId == null)
            {
                return Unauthorized(new { message = "User not authenticated" });
            }
            try
            {
                var post = await _postService.GetPostByIdAsync(postId);
                if (post == null) return NotFound();
                if (post.UserId != currentUserId.Value)
                {
                    return Unauthorized(new { message = "Only creator can delete" });
                }

                await _postService.DeletePostAsync(postId);
                return Ok(new { message = "Xóa bài viết thành công." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }

        }

    }

}
