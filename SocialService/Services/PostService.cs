using GrpcService;
using Share.GrpcClient;
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
        private readonly IGrpcClient _grpcClient;
        private readonly ICurrentUserService _currentUserService;

        public PostService(IPostRepository postRepository, IPostMediaRepository postMediaRepository, IMediaUploadService mediaUploadService, IGrpcClient grpcClient , ICurrentUserService currentUserService)
        {
            _postRepository = postRepository;
            _postMediaRepository = postMediaRepository;
            _mediaUploadService = mediaUploadService;
            _grpcClient = grpcClient ;
            _currentUserService = currentUserService;
        }
        public async Task<PostResponse> CreatePostAsync(Guid userId, PostRequest request)
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
                UserId = userId,
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
            var response = MapToResponse(newPost);
            await EnrichUserDataAsync(response);
            return response;
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
           var post = await _postRepository.GetByIdWithMediaAsync(postId);
            if (post == null)
            {
                return null;
            }
            var response = MapToResponse(post);
            await EnrichUserDataAsync(response);
            return response;
        }

        public async Task<IEnumerable<PostResponse>> GetPublicPostAsync(int take = 10, DateTime? before = null)
        {
            var posts = await _postRepository.GetPostAsync(take, before, null);

            var responseList = posts.Select(MapToResponse).ToList();

            // [UPDATE] Gọi gRPC song song cho cả list để tối ưu
            await EnrichUserDataForListAsync(responseList);

            return responseList;
        }
        //public async Task<IEnumerable<PostResponse>> GetPublicPostAsync(int take = 10, DateTime? before = null)
        //{
        //    // UserId = null => Lấy toàn bộ public
        //    var posts = await _postRepository.GetPostAsync(take, before, userId: null);
        //    return posts.Select(MapToResponse);
        //}

        public async Task<IEnumerable<PostResponse>> GetUserPostAsync(Guid userId, int take = 10, DateTime? before = null)
        {
            var posts = await _postRepository.GetPostAsync(take, before, userId);

            var responseList = posts.Select(MapToResponse).ToList();

            // [UPDATE] Gọi gRPC lấy thông tin user
            // Vì tất cả bài viết đều của 1 user, ta có thể tối ưu hơn nhưng dùng hàm chung vẫn ok
            await EnrichUserDataForListAsync(responseList);

            return responseList;
        }

        public async Task<PostResponse> UpdatePostAsync(Guid postId, PostUpdateRequest request)
        {
            // 1. Kiểm tra bài viết tồn tại
            var post = await _postRepository.GetByIdAsync(postId);
            if (post == null)
            {
                throw new Exception("Bài viết không tồn tại.");
            }

            // 2. [QUAN TRỌNG] Luôn load Media hiện tại từ DB và ép kiểu về List để thao tác trong bộ nhớ
            var currentMediasInDb = await _postMediaRepository.GetMediaByPostIdAsync(postId);
            post.PostMedias = currentMediasInDb.ToList(); // Chuyển thành List để dễ Add/Remove

            // 3. XỬ LÝ XÓA MEDIA CŨ
            if (request.DeletedMediaIds != null && request.DeletedMediaIds.Any())
            {
                // Lọc ra những item cần xóa mà thực sự đang có trong bài viết
                var mediaToDelete = post.PostMedias
                    .Where(m => request.DeletedMediaIds.Contains(m.Id))
                    .ToList();

                if (mediaToDelete.Any())
                {
                    // A. Xóa khỏi Database
                    _postMediaRepository.RemoveRange(mediaToDelete);

                    // B. [FIX] Xóa khỏi List trong bộ nhớ để Response trả về đúng
                    // Dùng RemoveAll cho an toàn
                    foreach (var item in mediaToDelete)
                    {
                        var itemToRemove = post.PostMedias.FirstOrDefault(x => x.Id == item.Id);
                        if (itemToRemove != null)
                        {
                            post.PostMedias.Remove(itemToRemove);
                        }
                    }
                }
            }

            // 4. XỬ LÝ UPLOAD MEDIA MỚI
            if (request.NewFiles != null && request.NewFiles.Any())
            {
                // Tính SortOrder tiếp theo
                int currentMaxSort = post.PostMedias.Any() ? post.PostMedias.Max(m => m.SortOrder) : -1;
                int nextSortOrder = currentMaxSort + 1;

                var newMediaList = new List<PostMedias>();

                foreach (var file in request.NewFiles)
                {
                    var ext = Path.GetExtension(file.FileName).ToLower();
                    string mediaUrl = "";
                    int mediaType = 0;

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

                    var newMedia = new PostMedias
                    {
                        Id = Guid.NewGuid(),
                        PostId = post.Id,
                        MediaUrl = mediaUrl,
                        MediaType = mediaType,
                        SortOrder = nextSortOrder++
                    };

                    newMediaList.Add(newMedia);

                    // [FIX] Add ngay vào list trong bộ nhớ để Response trả về có ảnh mới
                    post.PostMedias.Add(newMedia);
                }

                // Lưu xuống DB
                await _postMediaRepository.AddMediaRangeAsync(newMediaList);
            }

            // 5. Cập nhật thông tin Text
            if (!string.IsNullOrWhiteSpace(request.Content))
            {
                post.Content = request.Content;
            }

            if (request.PrivacyLevel.HasValue)
            {
                post.PrivacyLevel = request.PrivacyLevel.Value;
            }

            post.UpdatedAt = DateTime.UtcNow;

            // 6. Save Changes (Lưu tất cả thay đổi: Xóa ảnh, Thêm ảnh, Sửa text)
            _postRepository.Update(post);
            await _postRepository.SaveChangesAsync();

            // 7. Trả về kết quả (Lúc này post.PostMedias trong RAM đã chuẩn xác)
            var response = MapToResponse(post);
            await EnrichUserDataAsync(response);

            return response;
        }

        private async Task EnrichUserDataAsync(PostResponse post)
        {
            try
            {
                // Gọi qua Wrapper Client
                var result = await _grpcClient.GetUserByIdAsync(post.UserId.ToString());

                if (result.IsSuccess && result.Data != null)
                {
                    post.UserDisplayName = result.Data.DisplayName;
                    post.UserAvatar = result.Data.AvatarUrl;
                }
                else
                {
                    post.UserDisplayName = "Unknown User";
                    post.UserAvatar = ""; // Hoặc avatar mặc định
                }
            }
            catch
            {
                // Fallback nếu lỗi kết nối
                post.UserDisplayName = "Unknown User";
            }
        }

        private async Task EnrichUserDataForListAsync(List<PostResponse> posts)
        {
            if (!posts.Any()) return;

            // Chạy song song tất cả request để nhanh hơn
            var tasks = posts.Select(post => EnrichUserDataAsync(post));
            await Task.WhenAll(tasks);
        }

        private bool IsVideo(string extension)
        {
            return extension == ".mp4" || extension == ".mov" || extension == ".avi" || extension == ".mkv";
        }
        private PostResponse MapToResponse(Posts post)
        {
            var currentUserId = _currentUserService.Id;
            return new PostResponse
            {
                Id = post.Id,
                UserId = post.UserId,

                // Mặc định ban đầu (sẽ được điền sau bởi EnrichUserDataAsync)
                UserDisplayName = "Loading...",
                UserAvatar = "",

                Content = post.Content,
                PrivacyLevel = post.PrivacyLevel,
                TotalLikes = post.TotalLikes,
                TotalComments = post.TotalComments,
                CreatedAt = post.CreatedAt,
                IsLiked = post.Likes != null
                          && currentUserId.HasValue
                          && post.Likes.Any(l => l.UserId == currentUserId.Value),
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
