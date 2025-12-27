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
    public class CommentController : ControllerBase
    {
        private readonly ICommentService _commentService;
        private readonly ICurrentUserService _currentUserService;
        private readonly IPostService _postService;
        public CommentController(ICommentService commentService, IPostService postService, ICurrentUserService currentUserService)
        {
            _commentService = commentService;
            _postService = postService;
            _currentUserService = currentUserService;
        }
        [HttpPost("{postId}/comments")]
        public async Task<IActionResult> AddComment(CommentRequest request)
        {
            var currentUserId = _currentUserService.Id;
            if (currentUserId == null) return Unauthorized();
            try
            {
                request.UserId = currentUserId.Value;

                var comment = await _commentService.CreateCommentAsync(request);
                return Ok(comment);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }

        }
        [HttpDelete("comments/{commentId}")]
        public async Task<IActionResult> DeleteComment(Guid commentId)
        {
            var currentUserId = _currentUserService.Id;
            if (currentUserId == null) return Unauthorized();
            try
            {
                var comment = await _commentService.GetCommentByIdAsync(commentId);
                if (comment == null)
                {
                    return NotFound(new { message = "Bình luận không tồn tại." });
                }
                bool isCommentOwner = comment.UserId == currentUserId;

                // Quyền B: Người đang xóa là chủ bài viết?
                bool isPostOwner = false;

                if (!isCommentOwner)
                {
                    // Lưu ý: CommentResponse cần phải có PostId nhé. 
                    // Nếu chưa có, bạn cần vào CommentResponse.cs thêm: public Guid PostId { get; set; }
                    var post = await _postService.GetPostByIdAsync(comment.PostId);
                    if (post != null)
                    {
                        isPostOwner = post.UserId == currentUserId;
                    }
                }

                // Nếu không có quyền nào -> Chặn
                if (!isCommentOwner && !isPostOwner)
                {
                    return StatusCode(403, new { message = "Bạn không có quyền xóa bình luận này." });
                }

                // Bước 3: Thực hiện xóa
                await _commentService.DeleteCommentAsync(commentId);
                return Ok(new { message = "Xóa bình luận thành công." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
        [HttpGet("{postId}/comments")]
        public async Task<IActionResult> GetCommentsByPostId(Guid postId, int take = 10, DateTime? before = null)
        {
            try
            {
                var comments = await _commentService.GetCommentsByPostIdAsync(postId, take, before);
                return Ok(comments);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
        [HttpGet("comments/{commentId}")]
        public async Task<IActionResult> GetCommentById(Guid commentId)
        {
            try
            {
                var comment = await _commentService.GetCommentByIdAsync(commentId);
                return Ok(comment);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
        [HttpPut("comments/{commentId}")]
        public async Task<IActionResult> UpdateComment(Guid commentId, CommentUpdateRequest request)
        {
            var currentUserId = _currentUserService.Id;
            try
            {
                // Bước 1: Kiểm tra comment tồn tại
                var existingComment = await _commentService.GetCommentByIdAsync(commentId);
                if (existingComment == null) return NotFound();

                // Bước 2: Chỉ người viết mới được sửa (Chủ bài viết KHÔNG được sửa comment của người khác)
                if (existingComment.UserId != currentUserId)
                {
                    return StatusCode(403, new { message = "Bạn chỉ có thể chỉnh sửa bình luận của chính mình." });
                }

                var updatedComment = await _commentService.UpdateCommentAsync(commentId, request);
                return Ok(updatedComment);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
        [HttpGet("comments/{commentId}/replies")]
        public async Task<IActionResult> GetRepliesByCommentId(Guid commentId, int take = 10, DateTime? before = null)
        {
            try
            {
                var replies = await _commentService.GetRepliesByCommentIdAsync(commentId, take, before);
                return Ok(replies);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

    }
}
