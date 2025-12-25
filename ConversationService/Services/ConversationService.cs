using ChatRepository.Model.Request;
using ChatRepository.Model.Response;
using ChatRepository.Models;
using ChatRepository.Repositories;
using GrpcService;
using System.Net.WebSockets;
using System.Text.Json;
using UserService.Services;

namespace ChatService.Services
{
    public class ConversationService : IConversationService
    {
        private readonly IConversationRepository _conversationRepository;
        private readonly UserGrpcService.UserGrpcServiceClient _userGrpcClient;
        private readonly IUploadPhotoService _uploadPhotoService;
        private readonly NotificationGrpcService.NotificationGrpcServiceClient _notificationGrpcServiceClient;
        public ConversationService(IConversationRepository conversationRepository, UserGrpcService.UserGrpcServiceClient userGrpcServiceClient,IUploadPhotoService uploadPhotoService, NotificationGrpcService.NotificationGrpcServiceClient notificationGrpcServiceClient)
        {
            _conversationRepository = conversationRepository;
            _userGrpcClient = userGrpcServiceClient;
            _uploadPhotoService = uploadPhotoService;
            _notificationGrpcServiceClient = notificationGrpcServiceClient;
        }


        public async Task<ConversationResponse> CreateConversationAsync(ConversationCreateRequest request, Guid creatorId)
        {
            if(!request.IsGroup && request.IsPrivate)
            {
                return await CreatePrivateConversationAsync(request, creatorId);
            }
            else if (request.IsGroup)
            {
                return await CreateGroupConversationAsync(request, creatorId);
            }
            else
            {
                throw new InvalidOperationException("Invalid conversation type.");
            }
        }
        public async Task<ConversationResponse> CreatePrivateConversationAsync(ConversationCreateRequest request, Guid creatorId)
        {
            if (request.ParticipantIds == null || request.ParticipantIds.Count != 1)
                throw new InvalidOperationException("Private conversation must be exactly between 2 people.");
            Guid otherUserId = request.ParticipantIds.First();
            //tìm xem giữa 2 ng có đoạn chat hay ko
            var existing = await _conversationRepository.GetUserConversationsAsync(creatorId);
            var existingPrivate = existing.FirstOrDefault(c =>
            !c.IsGroup &&
            c.IsPrivate &&
            c.Participants.Count == 2 &&
            c.Participants.Any(p => p.UserId == creatorId) &&
            c.Participants.Any(p => p.UserId == otherUserId)
            );
            //nếu có thì sử dụng 
            if (existingPrivate != null)
            {
                return await MapToResponse(existingPrivate, creatorId);
            }
            //nếu chưa có thì tạo mới
            var conversation = new Conversations
            {
                Id = Guid.NewGuid(),
                Name = "Private Chat",
                IsGroup = false,
                IsPrivate = true,
                IsPrivateGroup = false,
                CreatedAt = DateTime.UtcNow,
                Participants = new List<Participants>
                {
                    new Participants
                    {
                        UserId = creatorId,
                        JoinAt = DateTime.UtcNow
                    },
                    new Participants
                    {
                        UserId = otherUserId,
                        JoinAt = DateTime.UtcNow
                    }
                }
            };
            await _conversationRepository.AddAsync(conversation);
            await _conversationRepository.SaveChangesAsync();
            return await MapToResponse(conversation, creatorId);
        }

        private async Task<ConversationResponse> CreateGroupConversationAsync(ConversationCreateRequest request, Guid creatorId)
        {
            var conversation = new Conversations
            {
                Id = Guid.NewGuid(),
                Name = request.Name,
                IsGroup = true,
                IsPrivate = false,
                IsPrivateGroup = request.IsPrivateGroup,
                CreatedAt = DateTime.UtcNow,
                AdminId = creatorId,
                Participants = new List<Participants>()
            };

            //add creator
            conversation.Participants.Add(new Participants
            {
                UserId = creatorId,
                JoinAt = DateTime.UtcNow
            });
            var newUserIds = new List<Guid>();
            //add other participants
            if (request.ParticipantIds != null)
            {
                foreach (var userId in request.ParticipantIds)
                {
                    if (userId != creatorId) // tránh thêm người tạo hai lần
                    {
                        conversation.Participants.Add(new Participants
                        {
                            UserId = userId,
                            JoinAt = DateTime.UtcNow
                        });
                    }
                }
            }
            await _conversationRepository.AddAsync(conversation);
            await _conversationRepository.SaveChangesAsync();
            // 🔔 Gửi thông báo cho từng user được thêm
            foreach (var userId in newUserIds)
            {
                await _notificationGrpcServiceClient.CreateUserNotificationAsync(
                    new CreateUserNotificationGrpcRequest
                    {
                        ConversationId = conversation.Id.ToString(),
                        ReceiverId = userId.ToString(),
                        Type = "System",
                        DataJson = JsonSerializer.Serialize(new
                        {
                            Title = "Join",
                            Content = $"You have been added to group '{conversation.Name}'",
                            GroupId = conversation.Id
                        })
                    });
            }
            return await MapToResponse(conversation, creatorId);

          

        }

        public async Task DeleteConversationAsync(Guid id)
        {
            var conversation = await _conversationRepository.GetByIdAsync(id);
            if (conversation == null) throw new KeyNotFoundException("Conversation not found.");
             _conversationRepository.Remove(conversation);
            await _conversationRepository.SaveChangesAsync();
        }


        public async Task<IEnumerable<ConversationResponse>> SearchConversationsAsync(Guid userId, string conversationName)
        {
            var conversations = await _conversationRepository.SearchConversationsAsync(userId, conversationName);
            return await Task.WhenAll(conversations.Select(c => MapToResponse(c, userId)));
        }

        public async Task UpdateConversationAsync(Guid id,ConversationUpdateRequest request, Guid adminGroupId)
        {
            var conversation = await _conversationRepository.GetByIdAsync(id);
            if (conversation == null) throw new KeyNotFoundException("Conversation not found.");
            if (conversation.AdminId != adminGroupId) throw new UnauthorizedAccessException("Only admin can update conversation.");

            conversation.Name = request.Name ?? conversation.Name;
            conversation.IsPrivateGroup = request.IsPrivateGroup;
            conversation.AdminId = request.AdminId ?? conversation.AdminId;
           if(request.AvartarGroup != null)
            {
                var avatarGroupUrl = _uploadPhotoService.UploadPhotoAsync(request.AvartarGroup);
                conversation.AvartarGroup = avatarGroupUrl;
            }
             _conversationRepository.Update(conversation);
            await _conversationRepository.SaveChangesAsync();
        }

        public async Task<ConversationResponse?> GetConversationByIdAsync(Guid id)
        {
            var conversation = await _conversationRepository.GetByIdAsync(id);
            if (conversation == null) return null;

            return await MapToResponse(conversation, Guid.Empty);
        }

        public async Task<IEnumerable<ConversationResponse>> GetUserConversationsAsync(Guid userId)
        {
            var conversations = await _conversationRepository.GetUserConversationsAsync(userId);
            var responses = new List<ConversationResponse>();
            foreach (var c in conversations)
            {
                responses.Add(await MapToResponse(c, userId));
            }
            return responses;
        }

        // ✅ MapToResponse: gọi sang gRPC để lấy thông tin user
        public async Task<ConversationResponse> MapToResponse(Conversations conversation, Guid currentUserId)
        {
            var response = new ConversationResponse
            {
                Id = conversation.Id,
                Name = conversation.Name,
                IsGroup = conversation.IsGroup,
                IsPrivate = conversation.IsPrivate,
                IsPrivateGroup =conversation.IsPrivateGroup,
                AdminId = conversation.AdminId,
                CreatedAt = conversation.CreatedAt,
                AvartarGroup = conversation.AvartarGroup,
                IsDissolve = conversation.IsDissolve,
                Participants = new List<ParticipantResponse>()
            };

            foreach (var p in conversation.Participants)
            {
                var grpcReply = await _userGrpcClient.GetUserByIdAsync(new GetUserByIdRequest
                {
                    Id = p.UserId.ToString()
                });

                response.Participants.Add(new ParticipantResponse
                {
                    UserId = Guid.Parse(grpcReply.Id),
                    JoinAt = p.JoinAt,
                    DisplayName = grpcReply.DisplayName,
                    AvatarUrl = grpcReply.AvatarUrl
                });
            }

            // Nếu là chat 1-1 → đổi tên thành tên user kia
            if (!conversation.IsGroup && conversation.Participants.Count == 2)
            {
                var otherUser = response.Participants.FirstOrDefault(u => u.UserId != currentUserId);
                if (otherUser != null)
                {
                    response.Name = otherUser.DisplayName;
                }
            }

            return response;
        }

        public async Task DissolveConversationAsync(Guid id)
        {
            var conversation = await _conversationRepository.GetByIdAsync(id);
            if (conversation == null)
                throw new KeyNotFoundException("Conversation not found.");

            if (!conversation.IsGroup)
                throw new InvalidOperationException("Only groups can be dissolved.");

            conversation.IsDissolve = true;
            _conversationRepository.Update(conversation);
            await _conversationRepository.SaveChangesAsync();
        }
    }
}
