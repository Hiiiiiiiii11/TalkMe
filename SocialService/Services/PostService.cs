using Share.Services;
using SocialRepository.Model;
using SocialRepository.Model.Request;
using SocialRepository.Model.Response;
using SocialRepository.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SocialService.Services
{
    public class PostService : IPostService
    {
        private readonly IPostRepository _postRepository;
        private readonly IPostMediaRepository _postMediaRepository;
        private readonly IMediaUploadService _mediaUploadService;

        public PostService(IPostRepository postRepository, IPostMediaRepository postMediaRepository, IMediaUploadService mediaUploadService)
        {
            _postRepository = postRepository;
            _postMediaRepository = postMediaRepository;
            _mediaUploadService = mediaUploadService;
        }
        public async Task<PostResponse> CreatePostAsync(PostRequest request)
        {
            // ---------------------------------------------------------
            // BƯỚC 1: UPLOAD MEDIA LÊN CLOUD TRƯỚC (Chưa đụng vào DB)
            // ---------------------------------------------------------
            var mediaEntities = new List<PostMedias>();

            // Tạo ID cho Post trước ở trong code để gán cho Media (dù chưa lưu DB)
            var newPostId = Guid.NewGuid();

            if (request.Files != null && request.Files.Any())
            {
                int sortOrder = 0;
                try
                {
                    foreach (var file in request.Files)
                    {
                        string mediaUrl = "";
                        int mediaType = 0;

                        var ext = Path.GetExtension(file.FileName).ToLower();

                        // Upload
                        if (IsVideo(ext))
                        {
                            mediaUrl = await _mediaUploadService.UploadPostVideoAsync(file);
                            mediaType = 1;
                        }
                        else
                        {
                            mediaUrl = await _mediaUploadService.UploadPostImageAsync(file);
                            mediaType = 0;
                        }

                        // Thêm vào list tạm
                        mediaEntities.Add(new PostMedias
                        {
                            Id = Guid.NewGuid(),
                            PostId = newPostId, // Gán ID đã generate
                            MediaUrl = mediaUrl,
                            MediaType = mediaType,
                            SortOrder = sortOrder++
                        });
                    }
                }
                catch (Exception ex)
                {
                    // LỖI UPLOAD:
                    // Tại đây Database chưa có gì cả, nên không cần rollback DB.
                    // (Nâng cao: Nếu muốn sạch sẽ hơn thì gọi API Cloudinary xóa những ảnh 
                    // đã lỡ upload thành công trong vòng lặp này, nhưng tạm thời bỏ qua cho đơn giản)

                    throw new Exception($"Upload thất bại, bài viết chưa được tạo. Chi tiết: {ex.Message}");
                }
            }

            // ---------------------------------------------------------
            // BƯỚC 2: TẠO ENTITY VÀ LƯU VÀO DB (1 TRANSACTION DUY NHẤT)
            // ---------------------------------------------------------
            var newPost = new Posts
            {
                Id = newPostId, // Dùng ID đã tạo bên trên
                UserId = request.UserId,
                Content = request.Content,
                PrivacyLevel = request.PrivacyLevel,
                CreatedAt = DateTime.UtcNow,
                TotalLikes = 0,
                TotalComments = 0,

                // EF Core thông minh sẽ tự động lưu luôn cả list PostMedias này
                // vào bảng PostMedias nhờ quan hệ (Navigation Property)
                PostMedias = mediaEntities
            };

            try
            {
                // Chỉ gọi AddAsync vào bảng Post
                await _postRepository.AddAsync(newPost);
                await _postRepository.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                // LỖI LƯU DB:
                // Lúc này ảnh đã lên Cloud nhưng DB lỗi.
                // Đây là trường hợp hiếm gặp (Rác trên Cloud). 
                // Post không được tạo -> User thấy lỗi -> OK.
                throw new Exception("Lỗi lưu bài viết vào cơ sở dữ liệu.");
            }

            // ---------------------------------------------------------
            // BƯỚC 3: TRẢ VỀ KẾT QUẢ
            // ---------------------------------------------------------
            return MapToResponse(newPost);
        }

        public async Task DeletePostAsync(Guid postId)
        {
            var post = await _postRepository.GetByIdAsync(postId);
            if (post == null)
            {
                throw new Exception("Bài đăng không tồn tại");
            }
            _postRepository.Remove(post);
            await _postRepository.SaveChangesAsync();
        }

        public async Task<PostResponse> GetPostByIdAsync(Guid postId)
        {
           var post = await _postRepository.GetByIdAsync(postId);
            if (post == null)
            {
                return null;
            }
            return MapToResponse(post);
        }

        public async Task<IEnumerable<PostResponse>> GetPublicPostAsync(int take = 10, DateTime? before = null)
        {
            var posts = await _postRepository.GetPostAsync(take, before, null);
            return posts.Select(MapToResponse);
        }
        //public async Task<IEnumerable<PostResponse>> GetPublicPostAsync(int take = 10, DateTime? before = null)
        //{
        //    // UserId = null => Lấy toàn bộ public
        //    var posts = await _postRepository.GetPostAsync(take, before, userId: null);
        //    return posts.Select(MapToResponse);
        //}

        public async Task<IEnumerable<PostResponse>> GetUserPostAsync(Guid userId, int take = 10, DateTime? before = null)
        {
            var posts =await _postRepository.GetPostAsync(take, before, userId);
            return posts.Select(MapToResponse);
        }

        public async Task<PostResponse> UpdatePostAsync(Guid postId, PostUpdateRequest request)
        {
            // 1. Kiểm tra bài viết tồn tại
            var post = await _postRepository.GetByIdAsync(postId);
            if (post == null)
            {
                throw new Exception("Bài viết không tồn tại."); // Hoặc return null tùy logic Controller
            }

            // 2. Load danh sách Media hiện tại của bài viết (Nếu chưa có thì load từ DB)
            // Lý do: Cần list này để biết cái nào cần xóa và tính SortOrder cho ảnh mới
            if (post.PostMedias == null)
            {
                post.PostMedias = (ICollection<PostMedias>)await _postMediaRepository.GetMediaByPostIdAsync(postId);
            }

            // 3. XỬ LÝ XÓA MEDIA CŨ (Nếu người dùng gửi lên danh sách ID cần xóa)
            if (request.DeletedMediaIds != null && request.DeletedMediaIds.Any())
            {
                // Lọc ra những media cần xóa đang thuộc về post này
                var mediaToDelete = post.PostMedias
                    .Where(m => request.DeletedMediaIds.Contains(m.Id))
                    .ToList();

                foreach (var media in mediaToDelete)
                {
                    // Xóa khỏi Database
                    // (Lưu ý: GenericRepo cần có hàm Delete hoặc Remove)
                     _postMediaRepository.Remove(media);
                    _postRepository.SaveChangesAsync();

                    // Xóa khỏi list in-memory để tí nữa map response không bị thừa
                    post.PostMedias.Remove(media);

                    // (Nâng cao: Có thể gọi _mediaUploadService để xóa file trên Cloudinary nếu cần tiết kiệm dung lượng)
                }
            }

            // 4. XỬ LÝ UPLOAD MEDIA MỚI (Append vào cuối danh sách)
            if (request.NewFiles != null && request.NewFiles.Any())
            {
                // Tính SortOrder tiếp theo: Lấy số lớn nhất hiện tại + 1
                int currentMaxSort = post.PostMedias.Any() ? post.PostMedias.Max(m => m.SortOrder) : -1;
                int nextSortOrder = currentMaxSort + 1;

                var newMediaList = new List<PostMedias>();

                foreach (var file in request.NewFiles)
                {
                    string mediaUrl = "";
                    int mediaType = 0;
                    var ext = Path.GetExtension(file.FileName).ToLower();

                    // Upload lên Cloudinary
                    if (IsVideo(ext))
                    {
                        mediaUrl = await _mediaUploadService.UploadPostVideoAsync(file);
                        mediaType = 1;
                    }
                    else
                    {
                        mediaUrl = await _mediaUploadService.UploadPostImageAsync(file);
                        mediaType = 0;
                    }

                    // Tạo Entity Media mới
                    var newMedia = new PostMedias
                    {
                        Id = Guid.NewGuid(),
                        PostId = post.Id,
                        MediaUrl = mediaUrl,
                        MediaType = mediaType,
                        SortOrder = nextSortOrder++ // Tăng dần thứ tự
                    };

                    newMediaList.Add(newMedia);
                }

                // Lưu các media mới vào DB
                await _postMediaRepository.AddMediaRangeAsync(newMediaList);

                // Thêm vào list in-memory để trả về Response đầy đủ
                foreach (var m in newMediaList)
                {
                    post.PostMedias.Add(m);
                }
            }

            // 5. Cập nhật thông tin Text & Save Post
            post.Content = request.Content;
            post.PrivacyLevel = request.PrivacyLevel;
            post.UpdatedAt = DateTime.UtcNow;

             _postRepository.Update(post);
            await _postRepository.SaveChangesAsync();

            // 6. Trả về kết quả
            return MapToResponse(post);
        }
        private bool IsVideo(string extension)
        {
            return extension == ".mp4" || extension == ".mov" || extension == ".avi" || extension == ".mkv";
        }
        private PostResponse MapToResponse(Posts post)
        {
            return new PostResponse
            {
                Id = post.Id,
                UserId = post.UserId,
                Content = post.Content,
                PrivacyLevel = post.PrivacyLevel,
                TotalLikes = post.TotalLikes,
                TotalComments = post.TotalComments,
                CreatedAt = post.CreatedAt,
                UpdatedAt = post.UpdatedAt,
                Medias = post.PostMedias?.Select(m => new PostMediaResponse
                {
                    Id = m.Id,
                    Url = m.MediaUrl,
                    MediaType = m.MediaType == 1 ? "Video" : "Image",
                    SortOrder = m.SortOrder
                }).OrderBy(x => x.SortOrder).ToList() ?? new List<PostMediaResponse>()
            };
        }
    }
}
