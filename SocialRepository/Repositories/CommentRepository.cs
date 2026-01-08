using Microsoft.EntityFrameworkCore;
using Share.Repoitories;
using SocialRepository.Data;
using SocialRepository.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SocialRepository.Repositories
{
    public class CommentRepository : GenericRepository<Comments>, ICommentRepository
    {
        private readonly SocialDbContext _context;
        public CommentRepository(SocialDbContext context) : base(context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Comments>> GetCommentsAsync(Guid postId, int take = 10, DateTime? before = null)
        {
            var query = _context.Comments
                .AsNoTracking()
                .Where(c => c.PostId == postId && c.ParentCommentId == null);

            if (before.HasValue)
            {
                query = query.Where(c => c.CreatedAt < before.Value);
            }
            var comments = await query
                .OrderByDescending(c => c.CreatedAt)
                .Take(take)
                .ToListAsync();

            return comments;
        }
        public async Task<IEnumerable<Comments>> GetRepliesAsync(Guid parentCommentId, int take = 10, DateTime? before = null)
        {
            var query = _context.Comments
                .AsNoTracking()
                .Where(c => c.ParentCommentId == parentCommentId); // Lấy theo cha

            if (before.HasValue)
            {
                // Với Reply, thường người ta muốn xem cũ nhất trước (theo thứ tự hội thoại)
                // Nhưng để đồng bộ logic lazy load cuộn lên, ta cứ giữ CreatedAt < before
                query = query.Where(c => c.CreatedAt < before.Value);
            }

            return await query
                .OrderBy(c => c.CreatedAt) // Reply thường sắp xếp Cũ -> Mới (A nói -> B trả lời)
                .Take(take)
                .ToListAsync();
        }
        public async Task<int> CountRepliesAsync(Guid commentId)
        {
            return await _context.Comments.CountAsync(c => c.ParentCommentId == commentId);
        }
    }
}
