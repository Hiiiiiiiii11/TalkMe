using SocialRepository.Model.Request;
using SocialRepository.Model.Response;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SocialService.Services
{
    public interface ICommentService
    {
        // Lấy danh sách comment (cấp 1) của một post
        Task<IEnumerable<CommentResponse>> GetCommentsByPostIdAsync(Guid postId, int take = 10, DateTime? before = null);

        // Lấy danh sách câu trả lời (Reply - Cấp 2) của một comment
        Task<IEnumerable<CommentResponse>> GetRepliesByCommentIdAsync(Guid commentId, int take = 10, DateTime? before = null);

        Task<CommentResponse> GetCommentByIdAsync(Guid commentId);

        Task<CommentResponse> CreateCommentAsync(CommentRequest request);

        Task<CommentResponse> UpdateCommentAsync(Guid commentId, CommentUpdateRequest request);

        Task DeleteCommentAsync(Guid commentId);
    }
}
