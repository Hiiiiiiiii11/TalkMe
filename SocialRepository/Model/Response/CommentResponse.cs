using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SocialRepository.Model.Response
{
    public class CommentResponse
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }

        // [QUAN TRỌNG] Thông tin user lấy từ gRPC
        public Guid PostId { get; set; }
        public string UserDisplayName { get; set; }
        public string UserAvatarUrl { get; set; }
        public Guid? ParentCommentId { get; set; }

        public string Content { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        // Để UI biết có bao nhiêu câu trả lời mà hiện nút "Xem 3 câu trả lời..."
        public int ReplyCount { get; set; }
    }
}
