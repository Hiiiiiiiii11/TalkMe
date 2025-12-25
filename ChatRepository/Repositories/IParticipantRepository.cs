
using ChatRepository.Models;
using Share.Repoitories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatRepository.Repositories
{
    public interface IParticipantRepository : IGenericRepository<Participants>
    {
        Task<IEnumerable<Participants>> GetParticipantsByConversationIdAsync(Guid conversationId);
        Task<IEnumerable<Participants>> GetBannedParticipantsByConversationIdAsync(Guid conversationId);
        Task<IEnumerable<Participants>> GetBanChatParticipantsByConversationIdAsync(Guid conversationId);
        Task<Participants?> GetParticipantAsync(Guid conversationId, Guid userId);
        //Task AddParticipantAsync(Participants participant);
        //Task UpdateParticipantAsync(Participants participant);
        Task RemoveParticipantAsync(Guid conversationId, Guid userId);

        //Task SaveChangesAsync();

    }
}
