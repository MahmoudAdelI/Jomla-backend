using Jomla.Domain;
using Jomla.Domain.Entities;

namespace Jomla.Application.Common.Extensions;

public static class SortingExtensions
{
    public static IQueryable<SupplierOffer> ApplySorting(
        this IQueryable<SupplierOffer> query,
            OfferSortBy? sortBy,
        bool descending)
    {
       
        return sortBy switch
        {
            OfferSortBy.Title =>
                descending
                    ? query.OrderByDescending(x => x.Title)
                    : query.OrderBy(x => x.Title),

            OfferSortBy.Price =>
                descending
                    ? query.OrderByDescending(x => x.UnitPrice)
                    : query.OrderBy(x => x.UnitPrice),

            OfferSortBy.Discount =>
                descending
                    ? query.OrderByDescending(x => x.DiscountPercentage)
                    : query.OrderBy(x => x.DiscountPercentage),

            OfferSortBy.ExpiresAt =>
                descending
                    ? query.OrderByDescending(x => x.ExpiresAt)
                    : query.OrderBy(x => x.ExpiresAt),

            OfferSortBy.MostBuyers =>
                descending
                    ? query.OrderByDescending(x => x.Batches.Where(b => b.Status == BatchStatus.Open).SelectMany(b => b.Participants).Count(p => p.Status == BatchParticipantStatus.Active))
                    : query.OrderBy(x => x.Batches.Where(b => b.Status == BatchStatus.Open).SelectMany(b => b.Participants).Count(p => p.Status == BatchParticipantStatus.Active)),

            OfferSortBy.MostFilled =>
                descending
                    ? query.OrderByDescending(x => x.Batches.Where(b => b.Status == BatchStatus.Open).Sum(b => (int?)b.CurrentQuantity) ?? 0)
                    : query.OrderBy(x => x.Batches.Where(b => b.Status == BatchStatus.Open).Sum(b => (int?)b.CurrentQuantity) ?? 0),

            _ =>
                descending
                    ? query.OrderByDescending(x => x.CreatedAt)
                    : query.OrderBy(x => x.CreatedAt)
        };
    }
}