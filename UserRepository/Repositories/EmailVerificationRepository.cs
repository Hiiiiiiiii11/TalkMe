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
    public class EmailVerificationRepository : GenericRepository<EmailVerification>, IEmailVerificationRepository
    {
        private readonly UserDbContext _context;

        public EmailVerificationRepository(UserDbContext context) : base(context)
        {
            _context = context;
        }



        public async Task<EmailVerification?> GetByEmailAndCodeAsync(string email, string code)
        {
            return await _context.EmailVerifications
                .FirstOrDefaultAsync(e =>
                    e.Email == email &&
                    e.Code == code &&
                    e.IsVerified == false);
        }

        public async Task MarkAsVerifiedAsync(EmailVerification verification)
        {
            verification.IsVerified = true;
            _context.EmailVerifications.Update(verification);
            await _context.SaveChangesAsync();
        }
        public async Task<EmailVerification?> GetByEmailAsync(string email)
        {
            return await _context.EmailVerifications
                .OrderByDescending(e => e.ExpiredAt)
                .FirstOrDefaultAsync(e => e.Email == email);
        }
        public async Task DeleteAllByEmailAsync(string email)
        {
            var records = _context.EmailVerifications.Where(x => x.Email == email);
            _context.EmailVerifications.RemoveRange(records);
            await _context.SaveChangesAsync();
        }
    }
}
