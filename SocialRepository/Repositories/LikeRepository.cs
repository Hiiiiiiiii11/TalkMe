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
    public class LikeRepository : GenericRepository<Likes>, ILikeRepository
    {
        private readonly SocialDbContext _context;
        public LikeRepository(SocialDbContext context) : base(context)
        {
            _context = context;
        }

        public Task<Likes?> GetLikeByPostAndUserAsync(Guid postId, Guid userId)
        {
            return _context.Likes
                .FirstOrDefaultAsync(like => like.PostId == postId && like.UserId == userId);
        }
    }
}
