using Vakantiepark.Shared.DTOs;
using Vakantiepark.UI.Application.Helpers;

namespace Vakantiepark.UI.Application.Extensions
{
    public static class PagedRequestExtensions
    {
        // The paging part of a query string, shared by every paged list.
        public static QueryString ToPagingQuery(this PagedRequest request)
            => new QueryString()
                .Add("page", request.Page)
                .Add("pageSize", request.PageSize);
    }
}
