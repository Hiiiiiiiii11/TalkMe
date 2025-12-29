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
    public class PostRepository : GenericRepository<Posts>, IPostRepository
    {
        private readonly SocialDbContext _context;
        public PostRepository(SocialDbContext context) : base(context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Posts>> GetPostAsync(int take = 10, DateTime? before = null, Guid? userId = null)
        {
            var query = _context.Posts
                .AsNoTracking()
                .Include(p => p.PostMedias) // Load ảnh/video
                .AsQueryable();

            // 1. Lọc theo User (Nếu đang xem trang cá nhân)
            if (userId.HasValue)
            {
                query = query.Where(p => p.UserId == userId.Value);

                // Logic phụ: Nếu xem trang người khác thì chỉ hiện bài Public.
                // Nếu xem trang chính mình thì hiện cả Private. 
                // (Phần này bạn có thể xử lý thêm tùy nghiệp vụ, ở đây tôi lấy hết)

                //Để sau
            }

            // 2. Loại bỏ bài "Chỉ mình tôi" (Private) nếu đang xem Global Feed
            if (userId == null)
            {
                // PrivacyLevel: 0 = Public, 2 = Private
                // Chỉ lấy bài Public
                query = query.Where(p => p.PrivacyLevel == 0);
            }

            // 3. Lazy Load (Cursor)
            if (before.HasValue)
            {
                query = query.Where(p => p.CreatedAt < before.Value);
            }

            // 4. Sắp xếp & Lấy dữ liệu
            return await query
                .OrderByDescending(p => p.CreatedAt) // Mới nhất lên đầu
                .Take(take)
                .ToListAsync();
        }
    }
}
