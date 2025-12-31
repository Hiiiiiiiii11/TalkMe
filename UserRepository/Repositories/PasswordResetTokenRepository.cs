using Microsoft.EntityFrameworkCore;
using Share.Repoitories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UserRepository.Data;
using UserRepository.Models;


namespace UserRepository.Repositories
{
    public class PasswordResetTokenRepository : GenericRepository<PasswordResetToken>, IPasswordResetTokenRepository
    {
        public readonly UserDbContext _context;
        public PasswordResetTokenRepository(UserDbContext context) : base(context)
        {
            _context = context;
        }


        public async Task<PasswordResetToken?> GetValidTokenAsync(Guid userId, string token)
        {
            return await _context.PasswordResetTokens
             .FirstOrDefaultAsync(t => t.UserId == userId && t.Token == token && !t.IsUsed);
        }

        public async Task<PasswordResetToken?> GetUnusedValidTokenAsync(Guid userId, string token)
        {
            return await _context.PasswordResetTokens
                .FirstOrDefaultAsync(t => t.UserId == userId
                                           && t.Token == token
                                           && !t.IsUsed
                                           && t.ExpiredAt > DateTime.UtcNow);
        }
        public async Task DeleteTokensByUserIdAsync(Guid userId)
        {
            // Tìm tất cả token của user này
            var tokens = _context.PasswordResetTokens.Where(t => t.UserId == userId);

            // Xóa hết
            _context.PasswordResetTokens.RemoveRange(tokens);

            await _context.SaveChangesAsync();
        }


    }
}
