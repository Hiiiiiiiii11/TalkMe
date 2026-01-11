using ChatRepository.Model.Request;
using ChatRepository.Model.Response;
using ChatRepository.Models;
using ChatRepository.Repositories;
using GrpcService;
using Share.GrpcClient;
using System.Net.WebSockets;
using System.Text.Json;
using UserService.Services;

namespace ChatService.Services
{
    public class ConversationService : IConversationService
    {
        private readonly IConversationRepository _conversationRepository;
        private readonly IGrpcClient _grpcClient;
        private readonly IUploadPhotoService _uploadPhotoService;
        public ConversationService(IConversationRepository conversationRepository, IGrpcClient grpcClient,IUploadPhotoService uploadPhotoService, NotificationGrpcService.NotificationGrpcServiceClient notificationGrpcServiceClient)
        {
            _conversationRepository = conversationRepository;
            _grpcClient = grpcClient;
            _uploadPhotoService = uploadPhotoService;
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
                        newUserIds.Add(userId);
                    }
                }
            }
            await _conversationRepository.AddAsync(conversation);
            await _conversationRepository.SaveChangesAsync();
            // 🔔 Gửi thông báo cho từng user được thêm
            foreach (var userId in newUserIds)
            {
                var dataJson = JsonSerializer.Serialize(new
                {
                    Title = "Join",
                    Content = $"You have been added to group '{conversation.Name}'",
                    GroupId = conversation.Id
                });

                // Sử dụng _grpcClient thay vì gọi trực tiếp
                await _grpcClient.NotifyUserActionAsync(
                    conversation.Id.ToString(),
                    userId.ToString(),
                    "System",
                    dataJson
                );
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

        //public async Task<IEnumerable<ConversationResponse>> GetUserConversationsAsync(Guid userId)
        //{
        //    var conversations = await _conversationRepository.GetUserConversationsAsync(userId);
        //    var responses = new List<ConversationResponse>();
        //    foreach (var c in conversations)
        //    {
        //        responses.Add(await MapToResponse(c, userId));
        //    }
        //    return responses;
        //}

        public async Task<IEnumerable<ConversationResponse>> GetUserConversationsAsync(Guid userId)
        {
            var conversations = await _conversationRepository.GetUserConversationsAsync(userId);

            // Xử lý song song để tăng tốc độ map
            var tasks = conversations.Select(c => MapToResponse(c, userId));
            return await Task.WhenAll(tasks);
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
                IsPrivateGroup = conversation.IsPrivateGroup,
                AdminId = conversation.AdminId,
                CreatedAt = conversation.CreatedAt,
                AvartarGroup = conversation.AvartarGroup,
                IsDissolve = conversation.IsDissolve,
                Participants = new List<ParticipantResponse>()
            };

            // 1. Tạo danh sách các Task để gọi gRPC song song (nhanh hơn foreach await)
            var participantTasks = conversation.Participants.Select(async p =>
            {
                // Gọi qua Wrapper
                var grpcResult = await _grpcClient.GetUserByIdAsync(p.UserId.ToString());

                // Khởi tạo giá trị mặc định
                var displayName = "Unknown User";
                var avatarUrl = "";

                // Kiểm tra kết quả từ Wrapper
                if (grpcResult.IsSuccess && grpcResult.Data != null)
                {
                    displayName = grpcResult.Data.DisplayName;
                    avatarUrl = grpcResult.Data.AvatarUrl;
                }

                return new ParticipantResponse
                {
                    UserId = p.UserId,
                    JoinAt = p.JoinAt,
                    DisplayName = displayName,
                    AvatarUrl = avatarUrl
                };
            });

            // 2. Chờ tất cả các request gRPC hoàn tất
            var participantsData = await Task.WhenAll(participantTasks);
            response.Participants.AddRange(participantsData);

            // 3. Logic đổi tên nếu là chat 1-1
            if (!conversation.IsGroup && conversation.Participants.Count == 2 && currentUserId != Guid.Empty)
            {
                var otherUser = response.Participants.FirstOrDefault(u => u.UserId != currentUserId);
                if (otherUser != null)
                {
                    response.Name = otherUser.DisplayName;
                    // Có thể gán luôn Avatar của đoạn chat là avatar người kia
                    // response.AvartarGroup = otherUser.AvatarUrl; 
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
