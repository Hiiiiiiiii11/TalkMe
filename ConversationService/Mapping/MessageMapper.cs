using ChatRepository.Models.Response;
using ChatRepository.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatService.Mapping
{
    public static class MessageMapper
    {
        public static MessageResponse MapToResponse(this Messages message)
        {
            return new MessageResponse
            {
                Id = message.Id,
                ConversationId = message.ConversationId,
                SenderId = message.SenderId,
                Content = message.Content,
                SentAt = message.SentAt,
                IsEdited = message.IsEdited,
                IsDeleted = message.IsDeleted
            };
        }
    }
}
