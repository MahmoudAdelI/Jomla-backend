using Jomla.Application.Features.GroupRequests.Dtos;
using MediatR;

namespace Jomla.Application.Features.GroupRequests.Queries.GetGroupRequestOfferDetail;

public sealed record GetGroupRequestOfferDetailQuery(
    Guid OfferId,
    Guid SupplierId)
    : IRequest<SupplierGroupRequestOfferDetailDto>;