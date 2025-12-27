using Share.Repoitories;
using SocialRepository.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SocialRepository.Repositories
{
    public interface ILikeRepository : IGenericRepository<Likes>
    {
        Task<Likes?> GetLikeByPostAndUserAsync(Guid postId, Guid userId);
    }
}
