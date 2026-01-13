using System;

namespace Share.Helpers
{
    public static class PaginationHelper
    {
        private const int MAX_PAGE_SIZE = 20;

        // Hàm trả về Tuple (Skip, Take)
        public static (int Skip, int Take) CalculateSkipTake(int page, int pageSize)
        {
            if (page < 1) page = 1;

            // Giới hạn pageSize
            if (pageSize > MAX_PAGE_SIZE) pageSize = MAX_PAGE_SIZE;
            if (pageSize <= 0) pageSize = 10;

            int skip = (page - 1) * pageSize;
            return (skip, pageSize);
        }
    }
}