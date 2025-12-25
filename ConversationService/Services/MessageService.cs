using ChatRepository.Model.Request;
using ChatRepository.Models;
using ChatRepository.Models.Request;
using ChatRepository.Models.Response;
using ChatRepository.Repositories;
using ChatService.Mapping;
using ChatService.Repositories;
using GrpcService;
using Microsoft.AspNetCore.Components.Forms;
using Share.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatService.Services
{
    public class MessageService : IMessageService
    {
        private readonly IMessageRepository _messageRepository;
        private readonly IConversationRepository _conversationRepository;
        private readonly IConversationService _conversationService;
        private readonly NotificationGrpcService.NotificationGrpcServiceClient _notificationGrpcClient;
        private readonly UserGrpcService.UserGrpcServiceClient _userGrpcClient;
        public MessageService(IMessageRepository messageRepository, IConversationRepository conversationRepository, IConversationService conversationService, NotificationGrpcService.NotificationGrpcServiceClient notificationGrpcServiceClient,UserGrpcService.UserGrpcServiceClient userGrpcServiceClient )
        {
            _messageRepository = messageRepository;
            _conversationRepository = conversationRepository;
            _conversationService = conversationService;
            _notificationGrpcClient = notificationGrpcServiceClient;
            _userGrpcClient = userGrpcServiceClient;


        }
        public async Task<MessageResponse> SendGroupMessageAsync(SendGroupMessageRequest request, Guid senderId)
        {
            var conversation = await _conversationRepository.GetByIdAsync(request.ConversationId);

            if (conversation == null)
            {
                throw new Exception("Conversation not found.");
            }

            // 2. Kiểm tra trạng thái
            if (conversation.IsDissolve) 
            {
                throw new Exception("Conversation has been dissolved. Cannot send messages.");
            }
            var message = new Messages
            {
                ConversationId = request.ConversationId,
                SenderId = senderId,
                Content = request.Content,
                SentAt = DateTime.UtcNow,
                IsEdited = false,
                IsDeleted = false
            };
           await _messageRepository.AddAsync(message);
            await _messageRepository.SaveChangesAsync();

            //goi gRPC tạo notification
            await _notificationGrpcClient.CreateMessageNotificationAsync(new CreateMessageNotificationGrpcRequest
            {
                ConversationId = message.ConversationId.ToString(),
                MessageId = message.Id.ToString()
            });
            return message.MapToResponse();
        }
        public async Task<MessageResponse> SendPrivateMessageAsync(SendPrivateMessageRequest request, Guid senderId)
        {
            // Tạo conversation riêng (hoặc lấy conversation cũ)
            var conversationRequest = new ConversationCreateRequest
            {
                ParticipantIds = new List<Guid> { request.receiverId }
            };
            var receiverReply = await _userGrpcClient.GetUserByIdAsync(new GetUserByIdRequest
            {
                Id = request.receiverId.ToString()
            });
            if (receiverReply == null)
            {
                throw new Exception("User isn't exist");
            }

            var conversation = await _conversationService.CreatePrivateConversationAsync(conversationRequest, senderId);
            // Tạo tin nhắn
            var message = new Messages
            {
                Content = request.Content,
                ConversationId = conversation.Id,
                SenderId = senderId,
                SentAt = DateTime.UtcNow,
                IsEdited = false,
                IsDeleted = false
            };
            await _messageRepository.AddAsync(message);
            await _messageRepository.SaveChangesAsync();

            //goi gRPC tạo notification
            await _notificationGrpcClient.CreateMessageNotificationAsync(new CreateMessageNotificationGrpcRequest
            {
                ConversationId = message.ConversationId.ToString(),
                MessageId = message.Id.ToString()
            });
            return message.MapToResponse();
        }

        public async Task DeleteMessageAsync(Guid id)
        {
            var message = await _messageRepository.GetByIdAsync(id);
            if (message == null) throw new KeyNotFoundException("Message not found");
             _messageRepository.Remove(message);
            await _messageRepository.SaveChangesAsync();
        }

        public async Task DeleteMessageOnlyUserAsync(Guid id, Guid userId)
        {
            var message = await _messageRepository.GetByIdAsync(id);
            if (message == null) throw new KeyNotFoundException("Message not found");

            var deletion = new MessageDeletion
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                MessageId = message.Id,
                DeletedAt = DateTime.UtcNow
            };

            await _messageRepository.DeleteMessageOnlyUserAsync(deletion);
            await _messageRepository.SaveChangesAsync();
        }
           

        public async Task DeleteMessageWithAllAsync(Guid id)
        {
            var message = await _messageRepository.GetByIdAsync(id);
            if (message == null) throw new KeyNotFoundException("Message not found");

            message.IsDeleted = true;
            await _messageRepository.DeleteMessageWithAllAsync(message);
            await _messageRepository.SaveChangesAsync();
        }

        public async Task<MessageResponse?> GetMessageByIdAsync(Guid id)
        {
            var message = await _messageRepository.GetByIdAsync(id);
            return message?.MapToResponse();
        }

        public async Task<IEnumerable<MessageResponse>> GetMessageByRoomIdAsync(Guid conversationId, Guid currentUserId, int? take = null, DateTime? before = null)
        {
            var messages = await _messageRepository.GetMessagesByRoomIdAsync(conversationId, take, before);
            return messages.Select(m =>
            {
                string content;
                if ((m.IsDeleted))
                {
                    content = "Message has been removed.";
                }else if(m.MessageDeletions != null && m.MessageDeletions.Any(md => md.UserId == currentUserId))
                {
                    content = "You has been removed this message.";
                }
                else
                {
                    content = m.IsEdited ? $"{m.Content}(edited)": m.Content;
                }
                return new MessageResponse
                {
                    Id = m.Id,
                    ConversationId = m.ConversationId,
                    SenderId = m.SenderId,
                    SentAt = m.SentAt,
                    Content = content,
                    IsEdited = m.IsEdited,
                    IsDeleted = m.IsDeleted
                };
            });
        }

        public async Task<IEnumerable<MessageResponse>> SearchMessagesAsync(Guid conversationId, string keyword, int? take = null, DateTime? before = null)
        {
            var searchMessage = await _messageRepository.SearchMessageAsync(conversationId, keyword, take, before);
            return searchMessage.Select(m => m.MapToResponse());
        }

        public async Task EditMessageAsync(Guid id, EditMessageRequest request)
        {
            var message = await _messageRepository.GetByIdAsync(id);
            if (message == null)
                throw new KeyNotFoundException("Message not found.");
            message.Content = request.NewContent;
            message.IsEdited = true;
            _messageRepository.Update(message);
            await _messageRepository.SaveChangesAsync();
        }

        

    }
}
