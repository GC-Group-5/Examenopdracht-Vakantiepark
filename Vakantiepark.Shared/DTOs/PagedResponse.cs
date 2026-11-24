namespace Vakantiepark.Shared.DTOs
{
    // One page of a longer list, with what the frontend needs to show the page buttons.
    public class PagedResponse<T>
    {
        public List<T> Items { get; set; } = [];
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalCount { get; set; }

        public int TotalPages => PageSize == 0 ? 0 : (int)Math.Ceiling(TotalCount / (double)PageSize);
    }
}
