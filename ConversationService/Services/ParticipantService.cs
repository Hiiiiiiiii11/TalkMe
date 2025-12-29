


using ChatRepository.Model.Response;
using ChatRepository.Models;
using ChatRepository.Repositories;
using GrpcService;
using Microsoft.Extensions.Configuration.UserSecrets;
using Share.GrpcClient;
using System.Text.Json;

namespace ChatService.Services
{
    public class ParticipantService : IParticipantService
    {
        private readonly IParticipantRepository _participantRepository;
        private readonly IConversationRepository _conversationRepository;
        private readonly IGrpcClient _grpcClient;

        public ParticipantService(IParticipantRepository participantRepository, IConversationRepository conversationRepository, IGrpcClient grpcClient )
        {
            _participantRepository = participantRepository;
            _conversationRepository = conversationRepository;
            _grpcClient = grpcClient;
        }

        public async Task<List<Participants>> AddParticipantToConversation(Guid conversationId, List<Guid> userIds)
        {
            var conversation = await _conversationRepository.GetByIdAsync(conversationId);
            if (conversation == null)
            {
                throw new KeyNotFoundException("Conversation not found");
            }

            var addedParticipants = new List<Participants>();

            foreach (var userId in userIds)
            {
                if (userId == Guid.Empty) continue;
                // kiểm tra trùng
                var existing = await _participantRepository.GetParticipantAsync(conversationId, userId);
                if (existing != null) continue;

                var participant = new Participants
                {
                    ConversationId = conversationId,
                    UserId = userId,
                    IsBanned = false,
                    IsBanChat = false,
                    JoinAt = DateTime.UtcNow
                };
                await _participantRepository.AddAsync(participant);
                
                addedParticipants.Add(participant);

                // 🔔 Tạo thông báo "Tham gia nhóm"
                var dataJson = JsonSerializer.Serialize(new
                {
                    Title = "Join",
                    Content = $"You have been added to group '{conversation.Name}'",
                    GroupId = conversation.Id
                });

                // Gọi gRPC qua Wrapper (không cần await nếu không muốn chặn luồng, nhưng await cho an toàn)
                await _grpcClient.NotifyUserActionAsync(
                    conversation.Id.ToString(),
                    userId.ToString(),
                    "System",
                    dataJson
                );
                await _participantRepository.SaveChangesAsync();
            }
            return addedParticipants;
        }

        public async Task<IEnumerable<Participants>> BanChatParticipantsAsync(Guid conversationId, List<Guid> userIds)
        {
            var bannedChatUsers = new List<Participants>();

            foreach (var userId in userIds)
            {
                var user = await _participantRepository.GetParticipantAsync(conversationId, userId);
                if (user == null) continue;

                // Nếu đã ban rồi thì bỏ qua hoặc báo lỗi tùy logic (ở đây mình update nếu chưa ban)
                if (!user.IsBanChat)
                {
                    user.IsBanChat = true;
                    _participantRepository.Update(user);
                    bannedChatUsers.Add(user); // Thêm vào list trả về
                }
            }

            await _participantRepository.SaveChangesAsync();
            return bannedChatUsers;
        }

        public async Task<IEnumerable<Participants>> BannedParticipantsAsync(Guid conversationId, List<Guid> userIds)
        {
            var bannedUsers = new List<Participants>();

            foreach (var userId in userIds)
            {
                var user = await _participantRepository.GetParticipantAsync(conversationId, userId);
                if (user == null) continue;

                if (!user.IsBanned)
                {
                    user.IsBanned = true;
                    _participantRepository.Update(user);
                    bannedUsers.Add(user); // Thêm vào list trả về
                }
            }

            await _participantRepository.SaveChangesAsync();
            return bannedUsers;
        }

        public async Task<IEnumerable<Participants>> GetBanChatParticipantsByConversationIdAsync(Guid conversationId)
        {
            return await _participantRepository.GetBanChatParticipantsByConversationIdAsync(conversationId);
        }

        public async Task<IEnumerable<Participants>> GetBannedParticipantsByConversationIdAsync(Guid conversationId)
        {
            return await _participantRepository.GetBannedParticipantsByConversationIdAsync(conversationId);
        }

        public async Task<Participants?> GetParticipantAsync(Guid conversationId, Guid userId)
        {
            return await _participantRepository.GetParticipantAsync(conversationId, userId);
        }

        public async Task<IEnumerable<Participants>> GetParticipantsByConversationIdAsync(Guid conversationId)
        {
            return await _participantRepository.GetParticipantsByConversationIdAsync(conversationId);
        }

        public async Task<IEnumerable<Participants>> RemoveParticipantsAsync(Guid conversationId, List<Guid> userIds)
        {
            var conversation =await _conversationRepository.GetByIdAsync(conversationId);
            if(conversation == null)
            {
                throw new KeyNotFoundException("Conversation not found");
            }

            if (!conversation.IsGroup && conversation.IsPrivate) {
                throw new InvalidOperationException("Cannot remove participant from a private conversation.");
            }

            var removedUsers = new List<Participants>();
            foreach (var userId in userIds)
            {
                try
                {
                    // Lấy user trước để trả về info
                    var participant = await _participantRepository.GetParticipantAsync(conversationId, userId);
                    if (participant != null)
                    {
                        await _participantRepository.RemoveParticipantAsync(conversationId, userId);
                        removedUsers.Add(participant); // Thêm vào list trả về

                        // 🔔 (Tùy chọn) Gửi thông báo bạn đã bị xóa khỏi nhóm
                        var dataJson = JsonSerializer.Serialize(new
                        {
                            Title = "Kicked",
                            Content = $"You have been removed from group '{conversation.Name}'",
                            GroupId = conversation.Id
                        });

                        await _grpcClient.NotifyUserActionAsync(
                            conversation.Id.ToString(),
                            userId.ToString(),
                            "System",
                            dataJson
                        );
                    }
                }
                catch (KeyNotFoundException)
                {
                    continue;
                }
            }
            await _participantRepository.SaveChangesAsync();
            return removedUsers;
        }


        public async Task<IEnumerable<Participants>> UnBanChatParticipantsAsync(Guid conversationId, List<Guid> userIds)
        {
            var unbannedChatUsers = new List<Participants>();

            foreach (var userId in userIds)
            {
                var user = await _participantRepository.GetParticipantAsync(conversationId, userId);
                if (user == null) continue;

                if (user.IsBanChat)
                {
                    user.IsBanChat = false;
                    _participantRepository.Update(user);
                    unbannedChatUsers.Add(user); // Thêm vào list
                }
            }

            await _participantRepository.SaveChangesAsync();
            return unbannedChatUsers;
        }

        public async Task<IEnumerable<Participants>> UnBannedParticipantsAsync(Guid conversationId, List<Guid> userIds)
        {
            var unbannedUsers = new List<Participants>();
            foreach (var userId in userIds)
            {
                var user = await _participantRepository.GetParticipantAsync(conversationId, userId);
                if (user == null) continue;

                if (user.IsBanChat)
                {
                    user.IsBanned = false;
                    _participantRepository.Update(user);
                    unbannedUsers.Add(user); // Thêm vào list trả về
                }

            }
            await _participantRepository.SaveChangesAsync();
            return unbannedUsers;
            
        }
    }
}
