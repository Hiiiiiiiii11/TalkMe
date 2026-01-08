using Share.GrpcClient;
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
        private readonly IGrpcClient _grpcClient;
        public CommentService(ICommentRepository commentRepository, IPostRepository postRepository, IGrpcClient grpcClient)
        {
            _commentRepository = commentRepository;
            _postRepository = postRepository;
            _grpcClient = grpcClient;

        }
        public async Task<CommentResponse> CreateCommentAsync(Guid postId,Guid userId, CommentRequest request)
        {
            var post = await _postRepository.GetByIdAsync(postId);
            if (post == null) throw new Exception("Bài viết không tồn tại.");
            var newComment = new Comments
            {
                Id = Guid.NewGuid(),
                PostId = postId,
                UserId = userId,
                Content = request.Content,
                ParentCommentId = request.ParentCommentId, // Hỗ trợ Reply
                CreatedAt = DateTime.UtcNow
            };
            await _commentRepository.AddAsync(newComment);

            // D. [QUAN TRỌNG] Tăng biến đếm TotalComments của Post
            post.TotalComments++;
             _postRepository.Update(post);
            await _postRepository.SaveChangesAsync();

            var response = await MapToResponseAsync(newComment);
            await EnrichUserDataAsync(response);
            return response;
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
           var responseList = new List<CommentResponse>();
            foreach (var c in comments)
            {
                // Gọi MapToResponseAsync cho từng phần tử để lấy CountReplies
                responseList.Add(await MapToResponseAsync(c));
            }
            // Gọi hàm enrich user data cho danh sách
            await EnrichUserDataForListAsync(responseList);
            return responseList;
        }

        public async Task<IEnumerable<CommentResponse>> GetRepliesByCommentIdAsync(Guid commentId, int take = 10, DateTime? before = null)
        {
            // Bạn cần viết thêm hàm GetRepliesAsync trong Repository
            // Logic: Where(c => c.ParentCommentId == commentId)
            var replies = await _commentRepository.GetRepliesAsync(commentId, take, before);

           var responseList = new List<CommentResponse>();
            foreach (var c in replies)
            {
                // Gọi MapToResponseAsync cho từng phần tử để lấy CountReplies
                responseList.Add(await MapToResponseAsync(c));
            }
            // Gọi hàm enrich user data cho danh sách
            await EnrichUserDataForListAsync(responseList);
            return responseList;
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

            var response = await MapToResponseAsync(comment);
            await EnrichUserDataAsync(response);
            return response;
        }
        private async Task EnrichUserDataAsync(CommentResponse comment)
        {
            try
            {
                var result = await _grpcClient.GetUserByIdAsync(comment.UserId.ToString());

                if (result.IsSuccess && result.Data != null)
                {
                    comment.UserDisplayName = result.Data.DisplayName;
                    comment.UserAvatarUrl = result.Data.AvatarUrl;
                }
                else
                {
                    comment.UserDisplayName = "Unknown User";
                    comment.UserAvatarUrl = "";
                }
            }
            catch
            {
                comment.UserDisplayName = "Unknown User";
            }
        }

        private async Task EnrichUserDataForListAsync(List<CommentResponse> comments)
        {
            if (!comments.Any()) return;

            // Chạy song song để tối ưu tốc độ
            var tasks = comments.Select(c => EnrichUserDataAsync(c));
            await Task.WhenAll(tasks);
        }
        private async Task<CommentResponse> MapToResponseAsync(Comments comment)
        {
            int replyCount = 0;
            if (comment.ParentCommentId == null)
            {
                replyCount = await _commentRepository.CountRepliesAsync(comment.Id);
            }

            return new CommentResponse
            {
                Id = comment.Id,
                UserId = comment.UserId,
                PostId = comment.PostId, // Đừng quên map PostId

                // Mặc định ban đầu
                UserDisplayName = "Loading...",
                UserAvatarUrl = "",
                ParentCommentId = comment.ParentCommentId ?? null,
                Content = comment.Content,
                CreatedAt = comment.CreatedAt,
                UpdatedAt = comment.UpdatedAt,
                ReplyCount = replyCount
            };
        }
    }
}
