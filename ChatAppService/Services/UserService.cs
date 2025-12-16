using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UserRepository.Model.Request;
using UserRepository.Models;
using UserRepository.Repositories;
using UserService.Model.Response;

namespace UserService.Services
{
    public class UserService : IUserService
    {
        private readonly IUserRepository _userRepository;
        public readonly IUploadPhotoService _uploadPhotoService;
        public UserService(IUserRepository userRepository, IUploadPhotoService uploadPhotoService)
        {
            _userRepository = userRepository;
            _uploadPhotoService = uploadPhotoService;
        }



        public async Task<UserInfoResponse> AddUserAsync(RegisterUserRequest request)
        {
            var existUser = await _userRepository.GetUserByEmailAsync(request.Email);
            if (existUser != null)
            {
                throw new InvalidOperationException($"User with email {request.Email} already exists!");
            }
            string harshPassword = BCrypt.Net.BCrypt.HashPassword(request.Password);
            var newUser = new User
            {
                Email = request.Email,
                PasswordHash = harshPassword,
                DisplayName =request.DisplayName,
                CreatedAt = DateTime.UtcNow,
                AvatarUrl =null,
                IsActive = true


            };
            await _userRepository.AddAsync(newUser);
            await _userRepository.SaveChangesAsync();
            return MapToResponse(newUser);

        }
        public async Task<UserInfoResponse?> GetUserByIdAsync(Guid id)
        {
            var user = await _userRepository.GetByIdAsync(id);
            return user == null ? null : MapToResponse(user);
        }
        public async Task<IEnumerable<UserInfoResponse>> GetAllUsersAsync()
        {
            var users = await _userRepository.GetAllAsync();
            return users.Select(MapToResponse);
        }
        public async Task<UserInfoResponse> UpdateUserAsync(Guid id, UpdateUserRequest request)
        {
            var existingUser = await _userRepository.GetByIdAsync(id);
            if (existingUser == null)
            {
                throw new KeyNotFoundException("User not found");
            }

            if (!string.IsNullOrEmpty(request.DisplayName))
                existingUser.DisplayName = request.DisplayName;

            if ((request.AvatarFile != null))
            {
                // Gọi service upload ảnh để lấy link
                var avatarUrl = _uploadPhotoService.UploadPhotoAsync(request.AvatarFile);
                existingUser.AvatarUrl = avatarUrl;
            }

            _userRepository.Update(existingUser);
            await _userRepository.SaveChangesAsync();
            return MapToResponse(existingUser);
        }


        public async Task DeleteUserAsync(Guid id)
        {
            var user = await _userRepository.GetByIdAsync(id);
            if(user == null)
            {
                throw new KeyNotFoundException("User not found");
            }
            _userRepository.Remove(user);
            await _userRepository.SaveChangesAsync();
        }


        public async Task<IEnumerable<UserInfoResponse>> SearchUsersAsync(string searchTerm)
        {
            var users = await _userRepository.GetAllAsync();
            return users
                .Where(u => u.Email.Contains(searchTerm, StringComparison.OrdinalIgnoreCase)
                         || (u.DisplayName != null && u.DisplayName.Contains(searchTerm, StringComparison.OrdinalIgnoreCase)))
                .Select(MapToResponse);
        }

        public async Task UnActiveUser(Guid id)
        {
            var user = await _userRepository.GetByIdAsync(id) ?? throw new KeyNotFoundException("User not found.");
            if (!user.IsActive)
                throw new Exception("User is already inactive.");

            user.IsActive = false;
            _userRepository.Update(user);
            await _userRepository.SaveChangesAsync();
        }

       
        public async Task ActiveUser(Guid id)
        {
            var user = await _userRepository.GetByIdAsync(id) ?? throw new KeyNotFoundException("User not found.");
            if (!user.IsActive)
                throw new Exception("User is already active.");

            user.IsActive = true;
            _userRepository.Update(user);
            await _userRepository.SaveChangesAsync();
        }

        private static UserInfoResponse MapToResponse(User user) => new()
        {
            Id = user.Id,
            Email = user.Email,
            DisplayName = user.DisplayName,
            CreatedAt = user.CreatedAt,
            AvatarUrl = user.AvatarUrl,
            IsActive = user.IsActive
        };
    }
}
