
using ChatRepository.Models;
using Share.Repoitories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatRepository.Repositories
{
    public interface IConversationRepository : IGenericRepository<Conversations>
    {
        //Task<Conversations> GetConversationByIdAsync(Guid id);
        Task<IEnumerable<Conversations>> GetUserConversationsAsync(Guid userId, int? skip = null, int? take = null);

        // Cập nhật thêm skip và take
        Task<IEnumerable<Conversations>> SearchConversationsAsync(Guid userId, string conversationName, int? skip = null, int? take = null);

    }
}
