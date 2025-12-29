using SocialRepository.Model.Response;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SocialService.Services
{
    public interface ILikeService
    {
        Task<LikeResponse> ToggleLikeAsync(Guid postId, Guid userId);

        // Kiểm tra xem User A đã like bài B chưa (để tô màu nút Like trên UI)
        Task<bool> HasUserLikedPostAsync(Guid postId, Guid userId);
    }
}
