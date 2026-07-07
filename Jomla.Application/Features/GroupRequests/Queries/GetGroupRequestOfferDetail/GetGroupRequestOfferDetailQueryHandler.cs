using Jomla.Application.Common.Exceptions;
using Jomla.Application.Common.Interfaces;
using Jomla.Application.Features.GroupRequests.Dtos;
using Jomla.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Jomla.Application.Features.GroupRequests.Queries.GetGroupRequestOfferDetail;

public sealed class GetGroupRequestOfferDetailQueryHandler(
    IAppDbContext db,
    IIdentityService identityService) : IRequestHandler<GetGroupRequestOfferDetailQuery, SupplierGroupRequestOfferDetailDto>
{
    public async Task<SupplierGroupRequestOfferDetailDto> Handle(GetGroupRequestOfferDetailQuery request, CancellationToken cancellationToken)
    {
        var currentUserId = identityService.GetCurrentUserId();
        if (currentUserId == Guid.Empty)
            throw new UnauthorizedAccessException("User is not authenticated.");

        var offer = await db.GroupRequestOffers
            .AsSplitQuery()
            .Include(o => o.Supplier)
            .Include(o => o.GroupRequest)
                .ThenInclude(gr => gr.Participants)
            .Include(o => o.Responses)
                .ThenInclude(r => r.Buyer)
            .FirstOrDefaultAsync(o => o.Id == request.OfferId, cancellationToken);

        if (offer is null)
            throw new NotFoundException(nameof(GroupRequestOffer), request.OfferId);

        // Only the supplier who placed the offer is authorized to view this detail view
        if (offer.SupplierId != currentUserId)
            throw new ForbiddenException("You are not authorized to view this offer's details.");

        var responses = offer.Responses.Select(r =>
        {
            var participant = offer.GroupRequest.Participants
                .FirstOrDefault(p => p.BuyerId == r.BuyerId);
            
            var quantity = participant?.Quantity ?? 0;

            return new SupplierBuyerResponseDto(
                r.BuyerId,
                r.Buyer.FirstName + " " + r.Buyer.LastName,
                r.Buyer.Email ?? string.Empty,
                r.Response,
                r.RespondedAt,
                quantity
            );
        }).ToList();

        var supplierName = offer.Supplier.FirstName + " " + offer.Supplier.LastName;

        return new SupplierGroupRequestOfferDetailDto(
            offer.Id,
            offer.GroupRequestId,
            offer.SupplierId,
            supplierName,
            offer.UnitPrice,
            offer.MinUnitPrice,
            offer.CurrentUnitPrice,
            offer.QuantityAvailable,
            offer.MinFallbackQuantity,
            offer.VariantAttributes,
            offer.RoundNumber,
            offer.Status,
            offer.CreatedAt,
            offer.ExpiresAt,
            offer.AcceptedQuantity,
            responses
        );
    }
}
