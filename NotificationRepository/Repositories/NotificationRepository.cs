
using Microsoft.EntityFrameworkCore;
using NotificationRepository.Data;
using NotificationRepository.Models;
using Share.Repoitories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NotificationRepository.Repositories
{
    public class NotificationRepository : GenericRepository<Notification>, INotificationRepository

    {
        public readonly NotificationDbContext _context;
        public NotificationRepository(NotificationDbContext context) : base(context)
        {
            _context = context;
        }
        //public async Task AddAsync(Notification notification)
        //{
        //    await _context.Notifications.AddAsync(notification);
        //    await _context.SaveChangesAsync();
        //}

        //public async Task DeleteAsync(Guid id)
        //{
        //    var notification = await _context.Notifications.FindAsync(id);
        //    if(notification != null)
        //    {
        //        _context.Notifications.Remove(notification);
        //        await _context.SaveChangesAsync();
        //    }
        //    else
        //    {
        //        throw new KeyNotFoundException($"Notification with ID {id} not found.");
        //    }
        //}

        public async Task<IEnumerable<Notification>> GetAllAsynnc()
        {
            return await _context.Notifications.ToListAsync();
        }

        //public async Task<Notification?> GetByIdAsync(Guid id)
        //{
        //    return await _context.Notifications
        //        .FirstOrDefaultAsync(n => n.Id == id);
        //}

        public async Task<List<Notification>> GetByUserIdAsync(Guid userId)
        {
            return await _context.Notifications.Where(m => m.UserId == userId && !m.IsRead).ToListAsync();
        }
        public async Task<List<Notification>> GetByUserIdAsync(Guid userId, int skip, int take)
        {
            return await _context.Notifications
                .Where(n => n.UserId == userId)
                .OrderByDescending(n => n.CreatedAt)
                .Skip(skip)
                .Take(take)
                .ToListAsync();
        }

        public async Task<List<Notification>> GetByTypeAsync(Guid userId, string type, int skip, int take)
        {
            return await _context.Notifications
                .Where(n => n.UserId == userId && n.Type == type) // Filter System ngay tại đây
                .OrderByDescending(n => n.CreatedAt)
                .Skip(skip)
                .Take(take)
                .ToListAsync();
        }

        //public async Task UpdateAsync(Notification notification)
        //{
        //    _context.Notifications.Update(notification);
        //    await _context.SaveChangesAsync();
        //}
    }
}
