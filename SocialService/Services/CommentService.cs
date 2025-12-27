using SocialRepository.Model;
using SocialRepository.Model.Request;
using SocialRepository.Model.Response;
using SocialRepository.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SocialService.Services
{
    public class CommentService : ICommentService
    {
        private readonly ICommentRepository _commentRepository;
        private readonly IPostRepository _postRepository;
        public CommentService(ICommentRepository commentRepository, IPostRepository postRepository)
        {
            _commentRepository = commentRepository;
            _postRepository = postRepository;
        }
        public async Task<CommentResponse> CreateCommentAsync(CommentRequest request)
        {
            var post = await _postRepository.GetByIdAsync(request.PostId);
            if (post == null) throw new Exception("Bài viết không tồn tại.");
            var newComment = new Comments
            {
                Id = Guid.NewGuid(),
                PostId = request.PostId,
                UserId = request.UserId,
                Content = request.Content,
                ParentCommentId = request.ParentCommentId, // Hỗ trợ Reply
                CreatedAt = DateTime.UtcNow
            };
            await _commentRepository.AddAsync(newComment);

            // D. [QUAN TRỌNG] Tăng biến đếm TotalComments của Post
            post.TotalComments++;
             _postRepository.Update(post);
            await _postRepository.SaveChangesAsync();
            return await MapToResponseAsync(newComment);
        }

        public async Task DeleteCommentAsync(Guid commentId)
        {
            var comment = await _commentRepository.GetByIdAsync(commentId);
            if (comment == null) throw new Exception("Comment không tồn tại.");

            // 1. Lấy Post để giảm số lượng
            var post = await _postRepository.GetByIdAsync(comment.PostId);

            // 2. Xóa comment
            _commentRepository.Remove(comment);

            // 3. Giảm biến đếm (nếu post còn tồn tại)
            if (post != null)
            {
                post.TotalComments--;
                if (post.TotalComments < 0) post.TotalComments = 0;
                 _postRepository.Update(post);
            }

            // 4. Lưu DB
            await _commentRepository.SaveChangesAsync();
        }

        public async Task<CommentResponse> GetCommentByIdAsync(Guid commentId)
        {
            var comment = await _commentRepository.GetByIdAsync(commentId);
            if (comment == null) return null;
            return await MapToResponseAsync(comment);

        }

        public async Task<IEnumerable<CommentResponse>> GetCommentsByPostIdAsync(Guid postId, int take = 10, DateTime? before = null)
        {
            // Gọi hàm Repo lấy comment gốc (ParentId = null)
            var comments = await _commentRepository.GetCommentsAsync(postId, take, before);
            return await MapListToResponseAsync(comments);
        }

        public async Task<IEnumerable<CommentResponse>> GetRepliesByCommentIdAsync(Guid commentId, int take = 10, DateTime? before = null)
        {
            // Bạn cần viết thêm hàm GetRepliesAsync trong Repository
            // Logic: Where(c => c.ParentCommentId == commentId)
            var replies = await _commentRepository.GetRepliesAsync(commentId, take, before);

            return await MapListToResponseAsync(replies);
        }

        public async Task<CommentResponse> UpdateCommentAsync(Guid commentId, CommentUpdateRequest request)
        {
            var comment = await _commentRepository.GetByIdAsync(commentId);
            if (comment == null) throw new Exception("Comment không tồn tại");

            // Chỉ cho phép sửa nội dung
            comment.Content = request.Content;
            comment.UpdatedAt = DateTime.UtcNow;

            _commentRepository.Update(comment);
            // Lưu ý: UpdateAsync của GenericRepo thường chưa SaveChanges, cần gọi thêm:
            await _commentRepository.SaveChangesAsync();

            return await MapToResponseAsync(comment);
        }
        private async Task<IEnumerable<CommentResponse>> MapListToResponseAsync(IEnumerable<Comments> comments)
        {
            var responseList = new List<CommentResponse>();
            foreach (var c in comments)
            {
                // Gọi MapToResponseAsync cho từng phần tử để lấy CountReplies
                responseList.Add(await MapToResponseAsync(c));
            }
            return responseList;
        }
        private async Task<CommentResponse> MapToResponseAsync(Comments comment)
        {
            // Logic tương tự hàm trên nhưng cho 1 object
            int replyCount = 0;
            // Chỉ đếm reply nếu đây là comment gốc (ParentId == null)
            if (comment.ParentCommentId == null)
            {
                replyCount = await _commentRepository.CountRepliesAsync(comment.Id);
            }

            return new CommentResponse
            {
                Id = comment.Id,
                UserId = comment.UserId,
                // UserDisplayName = ..., // TODO: Gọi gRPC User Service
                // UserAvatarUrl = ...,   // TODO: Gọi gRPC User Service
                Content = comment.Content,
                CreatedAt = comment.CreatedAt,
                UpdatedAt = comment.UpdatedAt,
                ReplyCount = replyCount
            };
        }
    }
}
