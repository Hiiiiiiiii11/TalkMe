
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
        Task<IEnumerable<Conversations>> GetUserConversationsAsync(Guid userId);
        //Task AddConversationAsync(Conversations conversation);
        //Task UpdateConversationAsync(Conversations conversation);
        //Task DeleteConversationAsync(Guid id);
        //Task SaveChangesAsync();
        //Task<Conversations?> GetPrivateConversationAsync(Guid userId1, Guid userId2);
        Task<List<Conversations>> SearchConversationsAsync(Guid userId, string conversationName);

    }
}
