
using GrpcService;
using NotificationRepository.Model.Request;
using NotificationRepository.Model.Response;
using NotificationRepository.Models;
using NotificationRepository.Repositories;
using NotificationService.Mapping;
using Share.GrpcClient;
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
        private readonly IGrpcClient _grpcClient;

        public NotificationService(INotificationRepository notificationRepository, IGrpcClient grpcClient)
        {
            _notificationRepository = notificationRepository;
            _grpcClient = grpcClient;



        }
        //tạo thông báo bất kì đến user(vd: mời vào nhóm, hệ thống,...)
        public async Task<NotificationResponse> CreateNotificationForUserAsync(CreateUserNotificationRequest request)
        {
            var notification = new Notification
            {
                Id = Guid.NewGuid(),
                UserId = request.receiverId,
                Type = request.Type ?? "System",
                ConversationId = request.ConversationId,
                DataJson = request.DataJson,
                CreatedAt = DateTime.UtcNow,
                IsRead = false
            };

            await _notificationRepository.AddAsync(notification);
            await _notificationRepository.SaveChangesAsync();

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
            //var conversationReply = await _conversationClient.GetConversationByIdAsync(
            //    new GetConversationByIdRequest { Id = request.ConversationId.ToString() });
            var convResult = await _grpcClient.GetConversationByIdAsync(request.ConversationId.ToString());
            if (!convResult.IsSuccess || convResult.Data == null)
            {
                throw new Exception($"Không tìm thấy hội thoại: {convResult.ErrorMessage}");
            }
            var conversationReply = convResult.Data;

            var msgResult = await _grpcClient.GetMessageByIdAsync(request.MessageId.ToString());
            if (!msgResult.IsSuccess || msgResult.Data == null)
            {
                throw new Exception($"Không tìm thấy tin nhắn: {msgResult.ErrorMessage}");
            }
            var messageReply = msgResult.Data;
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
            var receiverIdStr = conversationReply.Members.FirstOrDefault(m => m != messageReply.SenderId);
            if (string.IsNullOrEmpty(receiverIdStr))
                throw new Exception("Receiver not found in private chat");

            // Lấy thông tin người gửi qua Wrapper
            string senderName = "Unknown";
            string senderAvatar = "";

            var userResult = await _grpcClient.GetUserByIdAsync(messageReply.SenderId);
            if (userResult.IsSuccess && userResult.Data != null)
            {
                senderName = userResult.Data.DisplayName;
                senderAvatar = userResult.Data.AvatarUrl;
            }

            var notification = new Notification
            {
                Id = Guid.NewGuid(),
                UserId = Guid.Parse(receiverIdStr),
                ConversationId = request.ConversationId,
                MessageId = request.MessageId,
                Type = "Message",
                CreatedAt = DateTime.UtcNow,
                IsRead = false,

                // Chat riêng: Tiêu đề là Tên người gửi
                DataJson = JsonSerializer.Serialize(new
                {
                    ConversationName = senderName,
                    ConversationAvatar = senderAvatar,
                    MessageContent = messageReply.Content,
                    SentAt = messageReply.SentAt,
                    SenderId = messageReply.SenderId,
                    SenderName = senderName
                })
            };

            await _notificationRepository.AddAsync(notification);
            await _notificationRepository.SaveChangesAsync();

            return MapToMessageResponse(notification, senderName, senderAvatar, messageReply.Content, messageReply.SentAt);
        }

        private async Task<NotificationMessageResponse> CreateGroupMessageNotificationAsync(
            ConversationReply conversationReply,
            MessageReply messageReply,
            CreateMessageNotificationRequest request)
        {
            // Tất cả thành viên trừ sender
            string senderName = "Unknown";
            var userResult = await _grpcClient.GetUserByIdAsync(messageReply.SenderId);
            if (userResult.IsSuccess && userResult.Data != null)
            {
                senderName = userResult.Data.DisplayName;
            }
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
                    CreatedAt = DateTime.UtcNow,
                    IsRead = false,
                    DataJson = JsonSerializer.Serialize(new
                    {
                        ConversationName = conversationReply.Name,
                        ConversationAvatar = conversationReply.AvartarGroup,
                        MessageContent = $"{senderName}: {messageReply.Content}",
                        SentAt = messageReply.SentAt,
                        SenderId = messageReply.SenderId,
                        GroupId = conversationReply.Id
                    }),
                   
                };

                await _notificationRepository.AddAsync(notification);
                lastNotification = notification;
            }
            await _notificationRepository.SaveChangesAsync();
            if (lastNotification == null) return new NotificationMessageResponse();

            return MapToMessageResponse(lastNotification, conversationReply.Name, conversationReply.AvartarGroup, messageReply.Content, messageReply.SentAt);
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
        private NotificationMessageResponse MapToMessageResponse(Notification n, string convName, string convAvatar, string content, string sentAt)
        {
            return new NotificationMessageResponse
            {
                Id = n.Id,
                UserId = n.UserId,
                ConversationId = n.ConversationId,
                MessageId = n.MessageId,
                Type = n.Type,
                DataJson = n.DataJson,
                CreatedAt = n.CreatedAt,
                IsRead = n.IsRead,
                ConversationName = convName,
                ConversationAvatar = convAvatar,
                MessageContent = content,
                MessageSentAt = DateTime.TryParse(sentAt, out var d) ? d : DateTime.UtcNow
            };
        }
    }
}
