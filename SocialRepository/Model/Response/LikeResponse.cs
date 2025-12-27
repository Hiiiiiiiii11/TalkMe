using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SocialRepository.Model.Response
{
    public class LikeResponse
    {
        public bool IsLiked { get; set; } // true: Đã like, false: Đã hủy like
        public int NewTotalLikes { get; set; } // Số like cập nhật sau khi hành động
    }
}
