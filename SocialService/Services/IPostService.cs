using SocialRepository.Model.Request;
using SocialRepository.Model.Response;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SocialService.Services
{
    public interface IPostService
    {
        Task<IEnumerable<PostResponse>>GetPublicPostAsync(int take = 10, DateTime? before = null);
        Task<IEnumerable<PostResponse>> GetUserPostAsync(Guid userId, int take = 10, DateTime? before = null);
        Task<PostResponse> GetPostByIdAsync(Guid postId);
        Task<PostResponse> CreatePostAsync(Guid userId, PostRequest request); 
        Task<PostResponse> UpdatePostAsync(Guid postId, PostUpdateRequest request);
        Task DeletePostAsync(Guid postId);
    }
}
