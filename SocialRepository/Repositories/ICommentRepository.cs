using Share.Repoitories;
using SocialRepository.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SocialRepository.Repositories
{
    public interface ICommentRepository : IGenericRepository<Comments>
    {
        Task<IEnumerable<Comments>> GetCommentsAsync(Guid postId, int take = 10, DateTime? before = null);
        Task<IEnumerable<Comments>> GetRepliesAsync(Guid parentCommentId, int take = 10, DateTime? before = null);

        // [MỚI] Đếm số lượng reply của 1 comment (để hiện nút "Xem 3 câu trả lời")
        Task<int> CountRepliesAsync(Guid commentId);
    }
}
