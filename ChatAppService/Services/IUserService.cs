using Share.Models.Request;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UserRepository.Model.Request;
using UserRepository.Models;
using UserService.Model.Response;

namespace UserService.Services
{
    public interface IUserService
    {
        Task<UserInfoResponse> AddUserAsync(RegisterUserRequest request);
        Task<UserInfoResponse?> GetUserByIdAsync(Guid id);
        Task<IEnumerable<UserInfoResponse>> GetAllUsersAsync();
        Task<IEnumerable<UserInfoResponse>> SearchUsersAsync(string displayName, PagingRequest request);
        Task<UserInfoResponse> UpdateUserAsync(Guid id, UpdateUserRequest request);
        Task DeleteUserAsync(Guid id);
        Task UnActiveUser(Guid id);
        Task ActiveUser(Guid id);


    }
}
