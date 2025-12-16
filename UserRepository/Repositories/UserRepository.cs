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
    public class UserRepository : GenericRepository<User>, IUserRepository
    {
        private readonly UserDbContext _context;
        public UserRepository(UserDbContext context) : base(context)
        {
            _context = context;
        }

      

        //public async Task AddAsync(User user)
        //{
        //    await _context.Users.AddAsync(user);
        //    await _context.SaveChangesAsync();
        //}

        //public async Task DeleteAsync(Guid id)
        //{
        //    var user = await _context.Users.FindAsync(id);
        //    if(user != null)
        //    {
        //        _context.Users.Remove(user);
        //        await _context.SaveChangesAsync();
        //    }
        //    else
        //    {
        //        throw new KeyNotFoundException($"User with ID {id} not found.");
        //    }
        //}

        //public async Task<IEnumerable<User>> GetAllAsync()
        //{
        //    return await _context.Users.ToListAsync();
        //}

        //public async Task<User?> GetByIdAsync(Guid id)
        //{
        //   return await _context.Users
        //        .FirstOrDefaultAsync(u => u.Id == id);
        //}

        public async Task<User?> GetUserByEmailAsync(string email)
        {
            return await _context.Users
                .FirstOrDefaultAsync(u => u.Email == email);
        }

        public Task<List<User>> SearchAsync(string searchTerm)
        {
            return _context.Users
                .Where(u => u.DisplayName.Contains(searchTerm) || u.Email.Contains(searchTerm))
                .ToListAsync();
        }

        public Task UnActiveUser(Guid id)
        {
            var user = _context.Users.Find(id);
            if (user != null)
            {
                user.IsActive = false;
                _context.Users.Update(user);
            }
            return _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(User user)
        {
            _context.Users.Update(user);
            await _context.SaveChangesAsync();
        }
        public Task ActiveUser(Guid id)
        {
            var user = _context.Users.Find(id);
            if (user != null)
            {
                user.IsActive = true;
                _context.Users.Update(user);
            }
            return _context.SaveChangesAsync();
        }
    }
}
