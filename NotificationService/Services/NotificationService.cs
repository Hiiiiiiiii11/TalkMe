
using GrpcService;
using NotificationRepository.Model.Request;
using NotificationRepository.Model.Response;
using NotificationRepository.Models;
using NotificationRepository.Repositories;
using NotificationService.Mapping;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace NotificationService.Services
{
    public class NotificationService : INotificationService
    {
        private readonly INotificationRepository _notificationRepository;
        private readonly ConversationGrpcService.ConversationGrpcServiceClient _conversationClient;
        private readonly MessageGrpcService.MessageGrpcServiceClient _messageClient;
        private readonly UserGrpcService.UserGrpcServiceClient _userGrpcServiceClient;

        public NotificationService(INotificationRepository notificationRepository, ConversationGrpcService.ConversationGrpcServiceClient conversationGrpcServiceClient, MessageGrpcService.MessageGrpcServiceClient messageGrpcServiceClient , UserGrpcService.UserGrpcServiceClient userGrpcServiceClient)
        {
            _notificationRepository = notificationRepository;
            _conversationClient = conversationGrpcServiceClient;
            _messageClient = messageGrpcServiceClient;
            _userGrpcServiceClient = userGrpcServiceClient;

        }
        //tạo thông báo bất kì đến user(vd: mời vào nhóm, hệ thống,...)
        public async Task<NotificationResponse> CreateNotificationForUserAsync(CreateUserNotificationRequest request)
        {
            var notification = new Notification
            {
                UserId = request.receiverId,
                Type = "System",
                ConversationId = request.ConversationId,
                DataJson = request.DataJson,
                CreatedAt = DateTime.UtcNow,
                IsRead = false
            };
            await _notificationRepository.AddAsync(notification);
            return new NotificationResponse
            {
                Id = notification.Id,
                UserId = notification.UserId,
                Type = notification.Type,
                DataJson = notification.DataJson,
                CreatedAt = notification.CreatedAt,
                IsRead = notification.IsRead,
            };
        }


        //public async Task<NotificationMessageResponse> CreateMessageNotificationAsync(CreateMessageNotificationRequest request)
        //{
        //    var conversationReply = await _conversationClient.GetConversationByIdAsync(
        //    new GetConversationByIdRequest
        //    {
        //    Id = request.ConversationId.ToString()
        //    });
        //    var messageReply = await _messageClient.GetMessageByIdAsync(
        //    new GetMessageByIdRequest
        //    {
        //    Id = request.MessageId.ToString()
        //    });

        //    var targetUserIds = new List<Guid>();
        //    bool isPrivate = conversationReply.Members.Count == 2;

        //    if (isPrivate)
        //    {
        //        //private chat chỉ receiver nhận thông báo
        //        var receiverId = conversationReply.Members.FirstOrDefault(m => m != messageReply.SenderId);
        //        if(receiverId != null)
        //        {
        //            targetUserIds.Add(Guid.Parse(receiverId));
        //        }
        //    }
        //    else
        //    {
        //        // Group chat → tất cả trừ sender
        //        targetUserIds = conversationReply.Members
        //            .Where(m => m != messageReply.SenderId)
        //            .Select(Guid.Parse)
        //            .ToList();
        //    }

        //    Notification? lastNotification = null;
        //    // Thông tin hiển thị theo group và private chat
        //    string conversationName;
        //    string conversationAvatar;

        //    if (isPrivate)
        //    {
        //        var senderUser = await _userGrpcServiceClient.GetUserByIdAsync(new GetUserByIdRequest
        //        {
        //            Id = messageReply.SenderId
        //        });

        //        conversationName = senderUser.DisplayName;
        //        conversationAvatar = senderUser.AvatarUrl;
        //    }
        //    else
        //    {
        //        // Group → dùng info group
        //        conversationName = conversationReply.Name;
        //        conversationAvatar = conversationReply.AvartarGroup;
        //    }
        //    // Tạo notification cho từng user
        //    foreach (var userId in targetUserIds)
        //    {
        //        var notification = new Notification
        //        {
        //            UserId = userId,
        //            ConversationId = request.ConversationId,
        //            MessageId = request.MessageId,
        //            Type = "Message", // luôn là Message type
        //            DataJson = JsonSerializer.Serialize(new
        //            {
        //                ConversationName = conversationReply.Name,
        //                ConversationAvatar = conversationReply.AvartarGroup,
        //                MessageContent = messageReply.Content,
        //                messageReply.SentAt
        //            }),
        //            CreatedAt = DateTime.UtcNow,
        //            IsRead = false
        //        };
        //        await _notificationRepository.AddAsync(notification);
        //        lastNotification = notification;
        //    }
        //    return new NotificationMessageResponse
        //    {
        //        Id = lastNotification!.Id,
        //        UserId = lastNotification.UserId,
        //        ConversationId = lastNotification.ConversationId,
        //        MessageId = lastNotification.MessageId,
        //        Type = lastNotification.Type,
        //        DataJson = lastNotification.DataJson,
        //        CreatedAt = lastNotification.CreatedAt,
        //        IsRead = lastNotification.IsRead,
        //        ConversationName = conversationReply.Name,
        //        ConversationAvatar = conversationReply.AvartarGroup,
        //        MessageContent = messageReply.Content,
        //        MessageSentAt = DateTime.Parse(messageReply.SentAt)
        //    };

        //}
        //tách logic để dễ maintain
        public async Task<NotificationMessageResponse> CreateMessageNotificationAsync(CreateMessageNotificationRequest request)
        {
            var conversationReply = await _conversationClient.GetConversationByIdAsync(
                new GetConversationByIdRequest { Id = request.ConversationId.ToString() });

            var messageReply = await _messageClient.GetMessageByIdAsync(
                new GetMessageByIdRequest { Id = request.MessageId.ToString() });

            bool isPrivate = conversationReply.Members.Count == 2;

            if (isPrivate)
            {
                return await CreatePrivateMessageNotificationAsync(conversationReply, messageReply, request);
            }
            else
            {
                return await CreateGroupMessageNotificationAsync(conversationReply, messageReply, request);
            }
        }

        private async Task<NotificationMessageResponse> CreatePrivateMessageNotificationAsync(
            ConversationReply conversationReply,
            MessageReply messageReply,
            CreateMessageNotificationRequest request)
        {
            // Lấy receiver (khác với sender)
            var receiverId = conversationReply.Members.FirstOrDefault(m => m != messageReply.SenderId);
            if (receiverId == null) throw new Exception("Receiver not found for private chat");

            // Lấy thông tin người gửi
            var senderUser = await _userGrpcServiceClient.GetUserByIdAsync(new GetUserByIdRequest
            {
                Id = messageReply.SenderId
            });

            var notification = new Notification
            {
                UserId = Guid.Parse(receiverId),
                ConversationId = request.ConversationId,
                MessageId = request.MessageId,
                Type = "Message",
                DataJson = JsonSerializer.Serialize(new
                {
                    ConversationName = senderUser.DisplayName,  // ✅ tên người gửi
                    ConversationAvatar = senderUser.AvatarUrl, // ✅ avatar người gửi
                    MessageContent = messageReply.Content,
                    SentAt = messageReply.SentAt,
                    SenderName = senderUser.DisplayName,
                    SenderAvatar = senderUser.AvatarUrl
                }),
                CreatedAt = DateTime.UtcNow,
                IsRead = false
            };

            await _notificationRepository.AddAsync(notification);

            return new NotificationMessageResponse
            {
                Id = notification.Id,
                UserId = notification.UserId,
                ConversationId = notification.ConversationId,
                MessageId = notification.MessageId,
                Type = notification.Type,
                DataJson = notification.DataJson,
                CreatedAt = notification.CreatedAt,
                IsRead = notification.IsRead,
                ConversationName = senderUser.DisplayName,
                ConversationAvatar = senderUser.AvatarUrl,
                MessageContent = messageReply.Content,
                MessageSentAt = DateTime.Parse(messageReply.SentAt)
            };
        }

        private async Task<NotificationMessageResponse> CreateGroupMessageNotificationAsync(
            ConversationReply conversationReply,
            MessageReply messageReply,
            CreateMessageNotificationRequest request)
        {
            // Tất cả thành viên trừ sender
            var targetUserIds = conversationReply.Members
                .Where(m => m != messageReply.SenderId)
                .Select(Guid.Parse)
                .ToList();

            Notification? lastNotification = null;

            foreach (var userId in targetUserIds)
            {
                var notification = new Notification
                {
                    UserId = userId,
                    ConversationId = request.ConversationId,
                    MessageId = request.MessageId,
                    Type = "Message",
                    DataJson = JsonSerializer.Serialize(new
                    {
                        ConversationName = conversationReply.Name,
                        ConversationAvatar = conversationReply.AvartarGroup,
                        MessageContent = messageReply.Content,
                        SentAt = messageReply.SentAt
                    }),
                    CreatedAt = DateTime.UtcNow,
                    IsRead = false
                };

                await _notificationRepository.AddAsync(notification);
                lastNotification = notification;
            }

            return new NotificationMessageResponse
            {
                Id = lastNotification!.Id,
                UserId = lastNotification.UserId,
                ConversationId = lastNotification.ConversationId,
                MessageId = lastNotification.MessageId,
                Type = lastNotification.Type,
                DataJson = lastNotification.DataJson,
                CreatedAt = lastNotification.CreatedAt,
                IsRead = lastNotification.IsRead,
                ConversationName = conversationReply.Name,
                ConversationAvatar = conversationReply.AvartarGroup,
                MessageContent = messageReply.Content,
                MessageSentAt = DateTime.Parse(messageReply.SentAt)
            };
        }


        public async Task DeleteNotificationAsync(Guid id)
        {
            var notification = await _notificationRepository.GetByIdAsync(id);
            if (notification == null) return;
            _notificationRepository.Remove(notification);
            await _notificationRepository.SaveChangesAsync();
        }
        public async Task<IEnumerable<NotificationResponse>> GetAllNotificationsAsync()
        {
            var notifications = await _notificationRepository.GetAllAsynnc();
            return notifications.Select(n => n.ToResponse());
        }

        public async Task<NotificationResponse?> GetNotificationByIdAsync(Guid id)
        {
            var notification = await _notificationRepository.GetByIdAsync(id);
            return notification?.ToResponse();
        }

        public async Task<List<NotificationResponse>> GetNotificationsByUserIdAsync(Guid userId)
        {
            var notifications = await _notificationRepository.GetByUserIdAsync(userId);
            return notifications.Select(n => n.ToResponse()).ToList();
        }
        public async Task MarkAsReadAsync(Guid id)
        {
            var notification = await _notificationRepository.GetByIdAsync(id);
            if (notification == null) return;

            if (notification.Type == "Message")
            {
                // Nếu là thông báo tin nhắn thì xóa luôn
                _notificationRepository.Remove(notification);
                await _notificationRepository.SaveChangesAsync();
            }
            else if (notification.Type == "System")
            {
                // Nếu là thông báo hệ thống thì chỉ update IsRead = true
                notification.IsRead = true;
                 _notificationRepository.Update(notification);
                await _notificationRepository.SaveChangesAsync();
            }
        }

        public async Task UpdateNotificationAsync(Notification notification)
        {
             _notificationRepository.Update(notification);
            await _notificationRepository.SaveChangesAsync();
        }

        public async Task<NotificationMessageResponse?> GetNotificationMessageByIdAsync(Guid id)
        {
            var notification = await _notificationRepository.GetByIdAsync(id);
            if (notification == null || notification.Type != "Message") return null;

            return notification.ToMessageResponse();
        }

        public async Task<List<NotificationMessageResponse>> GetNotificationsMessageByUserIdAsync(Guid userId)
        {
            var notifications = await _notificationRepository.GetByUserIdAsync(userId);
            return notifications
                .Where(n => n.Type == "Message")
                .Select(n => n.ToMessageResponse())
                .ToList();
        }
        public async Task<List<NotificationMessageResponse>> GetNotificationsSystemByUserIdAsync(Guid userId)
        {
            var notifications = await _notificationRepository.GetByUserIdAsync(userId);
            return notifications
                .Where(n => n.Type == "System")
                .Select(n => n.ToMessageResponse())
                .ToList();
        }
    }
}
