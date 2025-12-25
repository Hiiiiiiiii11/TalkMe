using ChatRepository.Repositories;
using Grpc.Core;
using GrpcService;

namespace ChatService.Implement
{
    public class ConversationGrpcServiceImpl : ConversationGrpcService.ConversationGrpcServiceBase
    {
        private readonly IConversationRepository _conversationRepository;

        public ConversationGrpcServiceImpl(IConversationRepository conversationRepository)
        {
            _conversationRepository = conversationRepository;
        }

        public override async Task<ConversationReply> GetConversationById(GetConversationByIdRequest request, ServerCallContext context)
        {
            if (string.IsNullOrWhiteSpace(request.Id) || !Guid.TryParse(request.Id, out var conversationId))
                throw new RpcException(new Status(StatusCode.InvalidArgument, "Invalid conversation id"));

            var conversation = await _conversationRepository.GetByIdAsync(conversationId);
            if (conversation == null)
                throw new RpcException(new Status(StatusCode.NotFound, $"Conversation {conversationId} not found"));

            var reply = new ConversationReply
            {
                Id = conversation.Id.ToString(),
                Name = conversation.Name ?? string.Empty,
                AvartarGroup = conversation.AvartarGroup ?? string.Empty
            };

            // add members
            foreach (var p in conversation.Participants)
            {
                reply.Members.Add(p.UserId.ToString());
            }

            return reply;
        }
    }
}
