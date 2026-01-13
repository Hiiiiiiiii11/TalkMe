
using NotificationRepository.Models;
using Share.Repoitories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NotificationRepository.Repositories
{ 
    public interface INotificationRepository : IGenericRepository<Notification>
    {
        Task<IEnumerable<Notification>> GetAllAsynnc();
        //Task<Notification?> GetByIdAsync(Guid id);
        Task<List<Notification>> GetByUserIdAsync(Guid userId);
        Task<List<Notification>> GetByUserIdAsync(Guid userId, int skip, int take); // Lấy tất cả có phân trang
        Task<List<Notification>> GetByTypeAsync(Guid userId, string type, int skip, int take); // Lấy theo loại có phân trang
        //Task AddAsync(Notification notification);
        //Task UpdateAsync(Notification notification);
        //Task DeleteAsync(Guid id);
    }
}
