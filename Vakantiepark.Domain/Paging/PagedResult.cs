namespace Vakantiepark.Domain.Paging
{
    // One page of a longer list as a repository returns it, with the number of matches in total.
    public class PagedResult<T>
    {
        public List<T> Items { get; set; } = [];
        public int TotalCount { get; set; }
    }
}
