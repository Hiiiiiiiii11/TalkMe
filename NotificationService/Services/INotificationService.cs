using NotificationRepository.Model.Request;
using NotificationRepository.Model.Response;
using NotificationRepository.Models;
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
        Task<List<NotificationMessageResponse>> GetNotificationsByUserIdAsync(Guid userId);
        Task<NotificationMessageResponse?> GetNotificationMessageByIdAsync(Guid id);
        Task<List<NotificationMessageResponse>> GetNotificationsMessageByUserIdAsync(Guid userId);
        Task<List<NotificationMessageResponse>> GetNotificationsSystemByUserIdAsync(Guid userId);
        Task <NotificationResponse> CreateNotificationForUserAsync(CreateUserNotificationRequest request);

        Task<NotificationMessageResponse> CreateMessageNotificationAsync(CreateMessageNotificationRequest request);
        Task UpdateNotificationAsync(Notification notification);
        Task DeleteNotificationAsync(Guid id);
        Task MarkAsReadAsync(Guid id);
    }
}
