
using GrpcService;
using NotificationRepository.Model.Request;
using NotificationRepository.Model.Response;
using NotificationRepository.Models;
using NotificationRepository.Repositories;
using NotificationService.Mapping;
using Share.GrpcClient;
using Share.Helpers;
using Share.Models.Request;
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

            return notification.ToResponse();
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

            return notification.ToMessageResponse(senderName, senderAvatar);
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

            return lastNotification.ToMessageResponse(conversationReply.Name, conversationReply.AvartarGroup);
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

        public async Task<List<NotificationMessageResponse>> GetNotificationsByUserIdAsync(Guid userId, PagingRequest request)
        {
            // Tính toán Skip/Take
            var (skip, take) = PaginationHelper.CalculateSkipTake(request.Page, request.PageSize);

            // Gọi Repo (Repo cần cập nhật để nhận skip, take)
            // Giả sử: GetByUserIdAsync(Guid userId, int skip, int take)
            var notifications = await _notificationRepository.GetByUserIdAsync(userId, skip, take);

            return await MapNotificationsToResponseAsync(notifications);
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
            var messageNotifications = notifications.Where(n => n.Type == "Message").ToList();

            // Gọi hàm xử lý chung
            return await MapNotificationsToResponseAsync(messageNotifications);
        }
        public async Task<List<NotificationMessageResponse>> GetNotificationsSystemByUserIdAsync(Guid userId, PagingRequest request)
        {
            // Tính toán Skip/Take
            var (skip, take) = PaginationHelper.CalculateSkipTake(request.Page, request.PageSize);

            // ⚠️ QUAN TRỌNG: Bạn cần thêm hàm này vào Repository để lọc "System" ngay dưới DB trước khi phân trang
            // Nếu lấy hết về rồi mới .Where(System).Skip() ở RAM thì sẽ sai logic phân trang và chậm.
            var notifications = await _notificationRepository.GetByTypeAsync(userId, "System", skip, take);

            return await MapNotificationsToResponseAsync(notifications);
        }

        private async Task<List<NotificationMessageResponse>> MapNotificationsToResponseAsync(IEnumerable<Notification> notifications)
        {
            var tasks = notifications.Select(async n =>
            {
                string? fetchedName = null;
                string? fetchedAvatar = null;

                // Chỉ gọi gRPC khi cần thiết (System notification thiếu data)
                if (n.Type == "System" && !string.IsNullOrEmpty(n.DataJson))
                {
                    try
                    {
                        using (var doc = JsonDocument.Parse(n.DataJson))
                        {
                            if (doc.RootElement.TryGetProperty("GroupId", out var idProp))
                            {
                                var groupId = idProp.GetString();
                                if (!string.IsNullOrEmpty(groupId))
                                {
                                    var convResult = await _grpcClient.GetConversationByIdAsync(groupId);
                                    if (convResult.IsSuccess && convResult.Data != null)
                                    {
                                        fetchedName = convResult.Data.Name;
                                        fetchedAvatar = convResult.Data.AvartarGroup;
                                    }
                                }
                            }
                        }
                    }
                    catch { /* Ignore error */ }
                }

                // Gọi Mapper và truyền dữ liệu vừa lấy được vào
                return n.ToMessageResponse(fetchedName, fetchedAvatar);
            });

            var results = await Task.WhenAll(tasks);
            return results.OrderByDescending(x => x.CreatedAt).ToList();
        }
    }
}
