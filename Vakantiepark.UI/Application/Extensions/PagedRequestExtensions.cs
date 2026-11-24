using Vakantiepark.Shared.DTOs;

namespace Vakantiepark.UI.Application.Extensions
{
    public static class PagedRequestExtensions
    {
        // The paging part of a query string, shared by every paged list.
        public static List<string> ToPagingQuery(this PagedRequest request)
            => [$"page={request.Page}", $"pageSize={request.PageSize}"];
    }
}
