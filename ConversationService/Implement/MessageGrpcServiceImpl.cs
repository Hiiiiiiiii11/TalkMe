using ChatRepository.Repositories;
using Grpc.Core;
using GrpcService;

namespace ChatService.Implement
{
    public class MessageGrpcServiceImpl : MessageGrpcService.MessageGrpcServiceBase
    {
        private readonly IMessageRepository _messageRepository;

        public MessageGrpcServiceImpl(IMessageRepository messageRepository)
        {
            _messageRepository = messageRepository;
        }

        public override async Task<MessageReply> GetMessageById(GetMessageByIdRequest request, ServerCallContext context)
        {
            if (string.IsNullOrWhiteSpace(request.Id) || !Guid.TryParse(request.Id, out var messageId))
                throw new RpcException(new Status(StatusCode.InvalidArgument, "Invalid message id"));

            var message = await _messageRepository.GetByIdAsync(messageId);
            if (message == null)
                throw new RpcException(new Status(StatusCode.NotFound, $"Message {messageId} not found"));

            return new MessageReply
            {
                Id = message.Id.ToString(),
                SenderId = message.SenderId.ToString(),
                Content = message.Content,
                SentAt = message.SentAt.ToString("O") // ISO 8601 format
            };
        }
    }
}
