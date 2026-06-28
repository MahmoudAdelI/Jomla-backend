using Jomla.Domain;
using Jomla.Domain.Entities;

namespace Jomla.Application.Common.Extensions;

public static class OfferQueryExtensions
{
    public static IQueryable<SupplierOffer> ApplySearch(
        this IQueryable<SupplierOffer> query,
        string? search)
    {
        if (string.IsNullOrWhiteSpace(search))
            return query;

        search = search.Trim();

        return query.Where(x =>
            x.Title.Contains(search) ||
            (x.Description != null &&
             x.Description.Contains(search)));
    }

    public static IQueryable<SupplierOffer> ApplyCategoryFilter(
        this IQueryable<SupplierOffer> query,
        Guid? categoryId)
    {
        if (!categoryId.HasValue)
            return query;

        return query.Where(x => x.CategoryId == categoryId.Value);
    }

    public static IQueryable<SupplierOffer> ApplyStatusFilter(
        this IQueryable<SupplierOffer> query,
        SupplierOfferStatus? status)
    {
        if (!status.HasValue)
            return query;

        return query.Where(x => x.Status == status.Value);
    }
}