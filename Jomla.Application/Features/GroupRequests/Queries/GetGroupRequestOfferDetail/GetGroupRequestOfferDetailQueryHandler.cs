using Jomla.Application.Common.Exceptions;
using Jomla.Application.Common.Interfaces;
using Jomla.Application.Features.GroupRequests.Dtos;
using Jomla.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Jomla.Application.Features.GroupRequests.Queries.GetGroupRequestOfferDetail;

public sealed class GetGroupRequestOfferDetailQueryHandler(IAppDbContext db) : IRequestHandler<GetGroupRequestOfferDetailQuery, SupplierGroupRequestOfferDetailDto>
{
    public async Task<SupplierGroupRequestOfferDetailDto> Handle(
        GetGroupRequestOfferDetailQuery request,
        CancellationToken cancellationToken)
    {
        var offer = await db.GroupRequestOffers
            .AsNoTracking()
            .Include(x => x.Supplier)
            .Include(x => x.GroupRequest)
                .ThenInclude(x => x.Participants)
            .Include(x => x.Responses)
                .ThenInclude(x => x.Buyer)
            .FirstOrDefaultAsync(
                x => x.Id == request.OfferId,
                cancellationToken);

        if (offer is null)
        {
            throw new NotFoundException(
                nameof(GroupRequestOffer),
                request.OfferId);
        }

        if (offer.SupplierId != request.SupplierId)
        {
            throw new UnauthorizedAccessException();
        }

        return new SupplierGroupRequestOfferDetailDto(
            offer.Id,
            offer.GroupRequestId,
            offer.SupplierId,
            $"{offer.Supplier.FirstName} {offer.Supplier.LastName}",
            offer.UnitPrice,
            offer.MinUnitPrice,
            offer.CurrentUnitPrice,
            offer.QuantityAvailable,
            offer.MinFallbackQuantity,
            offer.AcceptedQuantity,
            offer.Status,
            offer.RoundNumber,
            offer.CreatedAt,
            offer.ExpiresAt,
            offer.VariantAttributes,
            offer.Responses
                .OrderBy(x => x.RespondedAt)
                .Select(x => new BuyerOfferResponseDto(
                    x.BuyerId,
                    $"{x.Buyer.FirstName} {x.Buyer.LastName}",
                    x.Response,
                    x.RespondedAt))
                .ToList());
    }
}