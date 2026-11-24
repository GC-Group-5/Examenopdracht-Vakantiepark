using Microsoft.EntityFrameworkCore;
using Vakantiepark.Domain.Paging;

namespace Vakantiepark.Infrastructure.Extensions
{
    public static class QueryablePagingExtensions
    {
        // Counts all matches, then fetches only the requested page. Order the query first,
        // otherwise the database may return rows in a different order on every page.
        public static async Task<PagedResult<T>> ToPagedResultAsync<T>(this IQueryable<T> query, int page, int pageSize)
        {
            int totalCount = await query.CountAsync();
            List<T> items = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return new PagedResult<T>() { Items = items, TotalCount = totalCount };
        }
    }
}
