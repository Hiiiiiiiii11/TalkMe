using Share.Repoitories;
using SocialRepository.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SocialRepository.Repositories
{
    public interface IPostMediaRepository : IGenericRepository<PostMedias>
    {
        Task AddMediaRangeAsync(IEnumerable<PostMedias> medias);
        Task<IEnumerable<PostMedias>> GetMediaByPostIdAsync(Guid postId);
    }
}
