using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NotificationRepository.Models
{
    public class Notification
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid UserId { get; set; }
        // "System" hoặc "Message"
        public string Type { get; set; } = string.Empty;
        public Guid ConversationId { get; set; }
        public Guid MessageId { get; set; }
        public string? DataJson { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public bool IsRead { get; set; } = false;

    }
}
