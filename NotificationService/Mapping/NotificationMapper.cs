using NotificationRepository.Model.Response;
using NotificationRepository.Models;
using System.Text.Json;

namespace NotificationService.Mapping
{
    public static class NotificationMapper
    {
        public static NotificationResponse ToResponse(this Notification notification)
        {
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

        // Thêm tham số optional (mặc định là null)
        public static NotificationMessageResponse ToMessageResponse(
            this Notification notification,
            string? overrideName = null,
            string? overrideAvatar = null)
        {
            // Dùng JsonDocument nhanh hơn và linh hoạt hơn Dictionary
            string jsonName = "";
            string jsonAvatar = "";
            string content = "";
            DateTime sentAt = notification.CreatedAt;

            if (!string.IsNullOrEmpty(notification.DataJson))
            {
                try
                {
                    using (var doc = JsonDocument.Parse(notification.DataJson))
                    {
                        var root = doc.RootElement;
                        // Thử lấy dữ liệu từ JSON
                        if (root.TryGetProperty("ConversationName", out var pName)) jsonName = pName.GetString() ?? "";
                        if (root.TryGetProperty("ConversationAvatar", out var pAvatar)) jsonAvatar = pAvatar.GetString() ?? "";

                        // Lấy content tùy key
                        if (root.TryGetProperty("MessageContent", out var pMsg)) content = pMsg.GetString() ?? "";
                        else if (root.TryGetProperty("Content", out var pContent)) content = pContent.GetString() ?? "";

                        if (root.TryGetProperty("SentAt", out var pTime) && DateTime.TryParse(pTime.GetString(), out var t)) sentAt = t;
                    }
                }
                catch { }
            }

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

                // Ưu tiên dữ liệu truyền vào (từ gRPC), nếu không có mới dùng từ JSON
                ConversationName = !string.IsNullOrEmpty(overrideName) ? overrideName : jsonName,
                ConversationAvatar = !string.IsNullOrEmpty(overrideAvatar) ? overrideAvatar : jsonAvatar,
                MessageContent = content,
                MessageSentAt = sentAt
            };
        }
    }
}