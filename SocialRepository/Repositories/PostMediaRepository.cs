using Share.Repoitories;
using SocialRepository.Data;
using SocialRepository.Model;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SocialRepository.Repositories
{
    public class PostMediaRepository : GenericRepository<PostMedias>, IPostMediaRepository
    {
        private readonly SocialDbContext _context;
        public PostMediaRepository(SocialDbContext context) : base(context)
        {
            _context = context;
        }

        public async Task AddMediaRangeAsync(IEnumerable<PostMedias> medias)
        {
            await _context.PostMedias.AddRangeAsync(medias);
        }

        public async Task<IEnumerable<PostMedias>> GetMediaByPostIdAsync(Guid postId)
        {
            return await _context.PostMedias
                 .AsNoTracking()
                 .Where(x => x.PostId == postId)
                 .OrderBy(x => x.SortOrder) // Quan trọng: Sắp xếp đúng thứ tự ảnh
                 .ToListAsync();
        }
    }
}
