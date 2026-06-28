namespace Jomla.Application.Common.Extensions;

public static class PaginationExtensions
{
    public static IQueryable<T> ApplyPagination<T>(
        this IQueryable<T> query,
        int? pageNumber,
        int? pageSize)
    {
        if (!pageNumber.HasValue || !pageSize.HasValue)
            return query;

        return query
            .Skip((pageNumber.Value - 1) * pageSize.Value)
            .Take(pageSize.Value);
    }
}