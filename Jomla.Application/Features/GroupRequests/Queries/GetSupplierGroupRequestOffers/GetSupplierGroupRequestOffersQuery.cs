using Jomla.Application.Features.GroupRequests.Dtos;
using Jomla.Domain;
using MediatR;
using System;

namespace Jomla.Application.Features.GroupRequests.Queries.GetSupplierGroupRequestOffers;

public sealed record GetSupplierGroupRequestOffersQuery(
    Guid SupplierId,
    int Page = 1,
    int PageSize = 10
) : IRequest<PagedResult<SupplierGroupRequestOfferDto>>;
