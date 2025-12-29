using Grpc.Core;
using GrpcService;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Share.GrpcClient
{
    public class GrpcClient : IGrpcClient
    {
 
        private readonly IServiceProvider _serviceProvider;

        public GrpcClient(
            IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }
        private T GetClient<T>()
        {
            // Tạo scope mới hoặc dùng scope hiện tại để lấy service
            // Dùng GetRequiredService để báo lỗi rõ ràng nếu quên đăng ký trong Program.cs
            return _serviceProvider.GetRequiredService<T>();
        }
        public async Task<GrpcResponse<UserReply>> GetUserByIdAsync(string userId)
        {
            return await ExecuteGrpcCall(async () =>
            {
                // Lấy client ra và dùng ngay lập tức
                var client = GetClient<UserGrpcService.UserGrpcServiceClient>();
                return await client.GetUserByIdAsync(new GetUserByIdRequest { Id = userId });
            });
        }

        // --- CHAT SERVICE ---
        public async Task<GrpcResponse<ConversationReply>> GetConversationByIdAsync(string conversationId)
        {
            return await ExecuteGrpcCall(async () =>
            {
                var client = GetClient<ConversationGrpcService.ConversationGrpcServiceClient>();
                return await client.GetConversationByIdAsync(new GetConversationByIdRequest { Id = conversationId });
            });
        }

        // --- CHAT SERVICE (Message) ---
        public async Task<GrpcResponse<MessageReply>> GetMessageByIdAsync(string messageId)
        {
            return await ExecuteGrpcCall(async () =>
            {
                var client = GetClient<MessageGrpcService.MessageGrpcServiceClient>();
                return await client.GetMessageByIdAsync(new GetMessageByIdRequest { Id = messageId });
            });
        }

        // --- NOTIFICATION SERVICE ---
        public async Task<GrpcResponse<NotificationMessageGrpcResponse>> NotifyNewMessageAsync(string conversationId, string messageId)
        {
            var request = new CreateMessageNotificationGrpcRequest
            {
                ConversationId = conversationId,
                MessageId = messageId
            };

            return await ExecuteGrpcCall(async () =>
            {
                var client = GetClient<NotificationGrpcService.NotificationGrpcServiceClient>();
                return await client.CreateMessageNotificationAsync(request);
            });
        }

        public async Task<GrpcResponse<NotificationMessageGrpcResponse>> NotifyUserActionAsync(string conversationId, string receiverId, string type, string dataJson)
        {
            var request = new CreateUserNotificationGrpcRequest
            {
                ConversationId = conversationId,
                ReceiverId = receiverId,
                Type = type,
                DataJson = dataJson ?? "{}"
            };

            return await ExecuteGrpcCall(async () =>
            {
                var client = GetClient<NotificationGrpcService.NotificationGrpcServiceClient>();
                return await client.CreateUserNotificationAsync(request);
            });
        }

        private async Task<GrpcResponse<T>> ExecuteGrpcCall<T>(Func<Task<T>> grpcCall)
        {
            try
            {
                var result = await grpcCall();
                return GrpcResponse<T>.Success(result);
            }
            catch (RpcException ex)
            {
                // Xử lý các lỗi chuẩn của gRPC
                if (ex.StatusCode == StatusCode.NotFound)
                {
                    return GrpcResponse<T>.Failure("Dữ liệu không tồn tại.", "NotFound");
                }
                if (ex.StatusCode == StatusCode.Unavailable)
                {
                    return GrpcResponse<T>.Failure("Service không phản hồi.", "Unavailable");
                }

                // Log lỗi tại đây nếu cần
                return GrpcResponse<T>.Failure($"Lỗi gRPC: {ex.Status.Detail}", ex.StatusCode.ToString());
            }
            catch (Exception ex)
            {
                return GrpcResponse<T>.Failure($"Lỗi hệ thống: {ex.Message}");
            }
        }
    }
}
