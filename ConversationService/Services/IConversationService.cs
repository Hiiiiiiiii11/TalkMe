using ChatRepository.Model.Request;
using ChatRepository.Model.Response;
using ChatRepository.Models;
using Share.Models.Request;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatService.Services
{
    public interface IConversationService
    {
        Task<ConversationResponse> GetConversationByIdAsync(Guid id);
        Task<IEnumerable<ConversationResponse>> GetUserConversationsAsync(Guid userId, PagingRequest request);
        Task<ConversationResponse> CreateConversationAsync(ConversationCreateRequest request, Guid creatorId);
        Task<ConversationResponse> CreatePrivateConversationAsync(ConversationCreateRequest request, Guid creatorId);
        Task UpdateConversationAsync(Guid id,ConversationUpdateRequest request, Guid adminGroupId);
        Task DeleteConversationAsync(Guid id);
        Task DissolveConversationAsync(Guid id);
        Task<IEnumerable<ConversationResponse>> SearchConversationsAsync(Guid userId, string conversationName, PagingRequest request);

    }
}
