using NotificationRepository.Model.Request;
using NotificationRepository.Model.Response;
using NotificationRepository.Models;
using Share.Models.Request;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NotificationService.Services
{
    public interface INotificationService
    {
        Task<IEnumerable<NotificationResponse>> GetAllNotificationsAsync();
        Task<NotificationResponse?> GetNotificationByIdAsync(Guid id);
        Task<List<NotificationMessageResponse>> GetNotificationsByUserIdAsync(Guid userId, PagingRequest request);
        Task<NotificationMessageResponse?> GetNotificationMessageByIdAsync(Guid id);
        Task<List<NotificationMessageResponse>> GetNotificationsMessageByUserIdAsync(Guid userId);
        Task<List<NotificationMessageResponse>> GetNotificationsSystemByUserIdAsync(Guid userId, PagingRequest request);
        Task <NotificationResponse> CreateNotificationForUserAsync(CreateUserNotificationRequest request);

        Task<NotificationMessageResponse> CreateMessageNotificationAsync(CreateMessageNotificationRequest request);
        Task UpdateNotificationAsync(Notification notification);
        Task DeleteNotificationAsync(Guid id);
        Task MarkAsReadAsync(Guid id);
    }
}
