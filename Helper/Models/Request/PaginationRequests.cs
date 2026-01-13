namespace Share.Models.Request
{
    // 1. Dùng cho danh sách tĩnh (Conversation, User list)
    public class PagingRequest
    {
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }

    // 2. Dùng cho Chat & Newsfeed (Message, Post)
    public class CursorPagingRequest
    {
        public int Take { get; set; } = 10;
        public DateTime? Before { get; set; } // Lấy dữ liệu cũ hơn mốc này
        public string? Keyword { get; set; }  // Hỗ trợ tìm kiếm luôn nếu cần
    }
}