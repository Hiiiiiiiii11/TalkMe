using GrpcService;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Share.GrpcClient
{
    public interface IGrpcClient
    {
        // User Service
        Task<GrpcResponse<UserReply>> GetUserByIdAsync(string userId);

        // Chat Service
        Task<GrpcResponse<ConversationReply>> GetConversationByIdAsync(string conversationId);
        Task<GrpcResponse<MessageReply>> GetMessageByIdAsync(string messageId);

        // Notification Service
        Task<GrpcResponse<NotificationMessageGrpcResponse>> NotifyNewMessageAsync(string conversationId, string messageId);
        Task<GrpcResponse<NotificationMessageGrpcResponse>> NotifyUserActionAsync(string conversationId, string receiverId, string type, string dataJson);
    }
}
