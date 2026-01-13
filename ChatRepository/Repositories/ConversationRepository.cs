
using ChatRepository.Data;
using ChatRepository.Model.Response;
using ChatRepository.Models;
using ChatService.Repositories;
using Microsoft.EntityFrameworkCore;
using Share.Repoitories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatRepository.Repositories
{
    public class ConversationRepository: GenericRepository<Conversations>, IConversationRepository
    {
        private readonly ChatDbContext _context;
        public ConversationRepository(ChatDbContext context) : base(context)
        {
            _context = context;
        }

        //public Task AddConversationAsync(Conversations conversation)
        //{
        //     _context.Conversations.AddAsync(conversation);
        //     return _context.SaveChangesAsync();
        //}

        //public async Task DeleteConversationAsync(Guid id)
        //{
        //    var conversation = await _context.Conversations
        //         .Include(c => c.Participants)
        //         .FirstOrDefaultAsync(c => c.Id == id);
        //    _context.Conversations.Remove(conversation);
        //    await _context.SaveChangesAsync();
        //}

        //public Task<Conversations> GetConversationByIdAsync(Guid id)
        //{
        //    return _context.Conversations
        //        .Include(c => c.Participants)
        //        .Include(c => c.Messages)
        //        .FirstOrDefaultAsync(c => c.Id == id);
        //}

        public async Task<IEnumerable<Conversations>> GetUserConversationsAsync(Guid userId, int? skip = null, int? take = null)
        {
            // Khởi tạo Query
            var query = _context.Conversations
                .Include(c => c.Participants) // Include bảng phụ
                .Where(c => c.Participants.Any(p => p.UserId == userId))
                .OrderByDescending(c => c.CreatedAt) // Bắt buộc sắp xếp trước khi phân trang
                .AsQueryable(); // Chuyển về IQueryable để nối chuỗi dynamic

            // Áp dụng Skip nếu có giá trị
            if (skip.HasValue)
            {
                query = query.Skip(skip.Value);
            }

            // Áp dụng Take nếu có giá trị
            if (take.HasValue)
            {
                query = query.Take(take.Value);
            }

            // Thực thi query
            return await query.ToListAsync();
        }

        //public Task SaveChangesAsync()
        //{
        //    return _context.SaveChangesAsync();
        //}

        //public Task<Conversations?> SearchConversationsAsync(Guid userId, string conversationName)
        //{
        //    return _context.Conversations.FirstOrDefaultAsync(c => c.Name == conversationName && c.Participants.Any(p => p.UserId == userId));
        //}


        //public Task UpdateConversationAsync(Conversations conversation)
        //{
        //    _context.Conversations.Update(conversation);
        //    return _context.SaveChangesAsync();
        //}

        public async Task<IEnumerable<Conversations>> SearchConversationsAsync(Guid userId, string conversationName, int? skip = null, int? take = null)
        {
            var query = _context.Conversations
                .Include(c => c.Participants)
                .Where(c => c.Participants.Any(p => p.UserId == userId) &&
                            c.Name.Contains(conversationName))
                .OrderByDescending(c => c.CreatedAt)
                .AsQueryable();

            if (skip.HasValue)
            {
                query = query.Skip(skip.Value);
            }

            if (take.HasValue)
            {
                query = query.Take(take.Value);
            }

            return await query.ToListAsync();
        }
    }
}
