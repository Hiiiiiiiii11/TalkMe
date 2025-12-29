using System;
using System.Collections.Generic;

namespace SocialRepository.Model.Response
{
    public class PostResponse
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public string UserDisplayName { get; set; }
        public string UserAvatar { get; set; }
        public string Content { get; set; }
        public int PrivacyLevel { get; set; }

        // Chỉ trả về số lượng, không trả về list object
        public int TotalLikes { get; set; }
        public int TotalComments { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; } // Nullable nếu chưa update

        // Map sang DTO nhỏ gọn hơn, không dùng Entity PostMedias trực tiếp
        public List<PostMediaResponse> Medias { get; set; } = new List<PostMediaResponse>();
    }

    public class PostMediaResponse
    {
        public Guid Id { get; set; }
        public string Url { get; set; }
        public string MediaType { get; set; } // "Image" hoặc "Video"
        public int SortOrder { get; set; }
    }
}