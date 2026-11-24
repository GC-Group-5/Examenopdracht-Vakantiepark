namespace Vakantiepark.Shared.DTOs
{
    // Base for every request of a paged list; the list's own filters go in a subclass.
    public class PagedRequest
    {
        public const int DefaultPageSize = 25;
        public const int MaximumPageSize = 100;

        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = DefaultPageSize;
    }
}
