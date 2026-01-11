using Share.Repoitories;
using SocialRepository.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SocialRepository.Repositories
{
    public interface IPostRepository : IGenericRepository<Posts>
    {
        Task<IEnumerable<Posts>> GetPostAsync(int take = 10, DateTime? before = null, Guid? userId = null);
        Task<Posts> GetByIdWithMediaAsync(Guid id);
    }
}
