using Microsoft.Extensions.Hosting;
using SocialRepository.Model;
using SocialRepository.Model.Response;
using SocialRepository.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SocialService.Services
{
    public class LikeService : ILikeService
    {
        private readonly ILikeRepository _likeRepository;
        private readonly IPostRepository _postRepository;
        public LikeService(ILikeRepository likeRepository, IPostRepository postRepository)
        {
            _likeRepository = likeRepository;
            _postRepository = postRepository;
        }

        public async Task<LikeResponse> ToggleLikeAsync(Guid postId, Guid userId)
        {
            var post = await _postRepository.GetByIdAsync(postId);
            if (post == null)
            {
                throw new Exception("Post not found");
            }
            var existingLike = await _likeRepository.GetLikeByPostAndUserAsync(postId, userId);
            bool isLikedNow = false;

            if (existingLike != null)
            {
                _likeRepository.Remove(existingLike);
                await _likeRepository.SaveChangesAsync();
                post.TotalLikes--;
                if (post.TotalLikes < 0) post.TotalLikes = 0;
                isLikedNow = false;
            }
            else
            {
                var newLike = new Likes
                {
                    Id = Guid.NewGuid(),
                    PostId = postId,
                    UserId = userId,
                    CreatedAt = DateTime.UtcNow
                };
                await _likeRepository.AddAsync(newLike);
                await _likeRepository.SaveChangesAsync();
                post.TotalLikes++;
                isLikedNow = true;
            }
            _postRepository.Update(post);
            await _postRepository.SaveChangesAsync();
            return new LikeResponse
            {
                IsLiked = isLikedNow,
                NewTotalLikes = post.TotalLikes
            };

        }

        public async Task<bool> HasUserLikedPostAsync(Guid postId, Guid userId)
        {
            // Kiểm tra xem record có tồn tại không
            var like = await _likeRepository.GetLikeByPostAndUserAsync(postId, userId);
            return like != null;
        }
    }
}
