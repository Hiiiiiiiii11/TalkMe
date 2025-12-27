using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SocialRepository.Model.Request
{
    public class PostRequest
    {
        public Guid UserId { get; set; }

        // Cho phép null vì có thể chỉ đăng ảnh không cần caption
        public string? Content { get; set; }

        // 0: Public, 1: Friends, 2: Private
        public int PrivacyLevel { get; set; } = 0;

        // Danh sách file (Ảnh/Video) upload lên
        public List<IFormFile>? Files { get; set; }
    }
    public class PostUpdateRequest
    {
        public string? Content { get; set; }
        public int PrivacyLevel { get; set; }

        // (Tùy chọn) Nếu bạn muốn chức năng Update cho phép thêm ảnh mới
        public List<IFormFile>? NewFiles { get; set; }

        // (Tùy chọn) Danh sách ID của các ảnh cũ muốn xóa đi
        public List<Guid>? DeletedMediaIds { get; set; }
    }
}
