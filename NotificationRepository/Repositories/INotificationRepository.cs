
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
        //Task AddAsync(Notification notification);
        //Task UpdateAsync(Notification notification);
        //Task DeleteAsync(Guid id);
    }
}
