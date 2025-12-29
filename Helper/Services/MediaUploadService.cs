using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Share.Services
{
    public class MediaUploadService : IMediaUploadService
    {
        private readonly Cloudinary _cloudinary;
        public MediaUploadService(Cloudinary cloudinary)
        {
            _cloudinary = cloudinary;
        }
        public async Task<string> UploadPostImageAsync(IFormFile file)
        {
            if (file == null || file.Length == 0)
                throw new ArgumentException("File ảnh không được để trống.");

            if (!file.ContentType.StartsWith("image/"))
                throw new ArgumentException("File không phải ảnh hợp lệ.");

            using var stream = file.OpenReadStream();

            var uploadParams = new ImageUploadParams
            {
                File = new FileDescription(file.FileName, stream),

                Folder = "fastchat/posts/images",

                Transformation = new Transformation()
                    .Quality("auto")
                    .FetchFormat("auto")   // ✅ ĐÚNG CÁCH
                    .Width(1080)
                    .Crop("limit")
            };

            var result = await _cloudinary.UploadAsync(uploadParams);

            if (result.Error != null)
                throw new Exception($"Lỗi upload ảnh: {result.Error.Message}");

            return result.SecureUrl.AbsoluteUri;
        }




        public async Task<string> UploadPostVideoAsync(IFormFile file)
        {
            if (file == null || file.Length == 0)
                throw new ArgumentException("File video không được để trống.");

            using var stream = file.OpenReadStream();

            var uploadParams = new VideoUploadParams
            {
                File = new FileDescription(file.FileName, stream),

                // Lưu vào thư mục riêng
                Folder = "fastchat/posts/videos",

                // EagerAsync: Xử lý video ngầm để API trả về nhanh hơn
                EagerAsync = true,

                // Nén video tự động
                Transformation = new Transformation().Quality("auto")
            };

            // UploadAsync xử lý bất đồng bộ, tốt cho Video dung lượng lớn
            var result = await _cloudinary.UploadAsync(uploadParams);

            if (result.Error != null)
                throw new Exception($"Lỗi upload video: {result.Error.Message}");

            return result.SecureUrl.AbsoluteUri;
        }
    }
}
