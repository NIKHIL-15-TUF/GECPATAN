namespace GECPatan.Core.Models.Common
{
    /// <summary>
    /// Generic paged list wrapper for Admin list views and API endpoints.
    /// Intended to be reused everywhere the original audit flagged an
    /// unbounded list (only 2 endpoints in the whole solution paginated
    /// before this) instead of every controller inventing its own paging.
    /// </summary>
    public class PagedResult<T>
    {
        public List<T> Items { get; init; } = new();
        public int Page { get; init; }
        public int PageSize { get; init; }
        public int TotalCount { get; init; }

        public int TotalPages => PageSize == 0 ? 0 : (int)Math.Ceiling(TotalCount / (double)PageSize);
        public bool HasPrevious => Page > 1;
        public bool HasNext => Page < TotalPages;
    }
}
