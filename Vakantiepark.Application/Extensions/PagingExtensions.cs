using Vakantiepark.Domain.Paging;
using Vakantiepark.Shared.DTOs;

namespace Vakantiepark.Application.Extensions
{
    public static class PagingExtensions
    {
        // Turns a repository page into the response, mapping every entity to its DTO.
        public static PagedResponse<TResponse> ToPagedResponse<TEntity, TResponse>(this PagedResult<TEntity> result, PagedRequest request, Func<TEntity, TResponse> map)
            => new PagedResponse<TResponse>()
            {
                Items = result.Items.Select(map).ToList(),
                Page = request.Page,
                PageSize = request.PageSize,
                TotalCount = result.TotalCount,
            };
    }
}
